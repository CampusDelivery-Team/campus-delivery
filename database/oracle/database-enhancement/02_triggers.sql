/*
  Member 2: automatic business-audit triggers.

  Design constraints:
  - Keep the existing 24-table model and existing audit tables.
  - Do not duplicate task_status_logs already written by the application.
  - Record PASS for recognized transitions and ABNORMAL otherwise.
  - Do not COMMIT, ROLLBACK, or reject the caller from an audit trigger.

  Execute as the owner of the business tables (APPUSER in the shared database).
*/

SET DEFINE OFF;

/* Type 1: audit the authoritative task-status log written by the application. */
CREATE OR REPLACE TRIGGER trg_task_status_audit
AFTER INSERT ON task_status_logs
FOR EACH ROW
DECLARE
    v_audit_id     audit_logs.audit_id%TYPE;
    v_audit_result audit_logs.audit_result%TYPE := 'ABNORMAL';
    v_note         audit_logs.exception_note%TYPE;
BEGIN
    IF (:NEW.status_before = 'WAITING' AND :NEW.status_after = 'ASSIGNED')
       OR (:NEW.status_before = 'ASSIGNED' AND :NEW.status_after = 'PICKED_UP')
       OR (:NEW.status_before = 'PICKED_UP' AND :NEW.status_after = 'DELIVERING')
       OR (:NEW.status_before = 'DELIVERING' AND :NEW.status_after = 'WAIT_CONFIRM')
       OR (:NEW.status_before = 'WAIT_CONFIRM' AND :NEW.status_after = 'WAIT_CONFIRM')
       OR (:NEW.status_before = 'WAIT_CONFIRM' AND :NEW.status_after = 'FINISHED')
       OR (:NEW.status_before = 'FINISHED' AND :NEW.status_after = 'REFUNDING')
       OR (:NEW.status_before = 'REFUNDING' AND :NEW.status_after = 'FINISHED')
       OR (:NEW.status_before = 'ASSIGNED' AND :NEW.status_after = 'ASSIGNED') THEN
        v_audit_result := 'PASS';
        v_note := SUBSTR(
            'AUTO_TASK_STATUS: ' || :NEW.status_before || ' -> ' || :NEW.status_after,
            1, 300);
    ELSE
        v_note := SUBSTR(
            'AUTO_TASK_STATUS: unrecognized transition ' ||
            NVL(:NEW.status_before, '(NULL)') || ' -> ' ||
            NVL(:NEW.status_after, '(NULL)'),
            1, 300);
    END IF;

    INSERT INTO audit_logs (audit_object, audit_result, audited_at, exception_note)
    VALUES ('LOG', v_audit_result, SYSDATE, v_note)
    RETURNING audit_id INTO v_audit_id;

    INSERT INTO audit_status_log_checks (audit_id, log_id)
    VALUES (v_audit_id, :NEW.log_id);
END;
/

/* Type 2b: audit refund creation and changes to critical refund fields. */
CREATE OR REPLACE TRIGGER trg_refund_change_audit
AFTER INSERT OR UPDATE OF
    payment_id,
    refund_amount,
    refund_reason,
    approved_amount,
    process_status
ON refunds
FOR EACH ROW
DECLARE
    v_audit_id        audit_logs.audit_id%TYPE;
    v_audit_result    audit_logs.audit_result%TYPE := 'PASS';
    v_note            audit_logs.exception_note%TYPE;
    v_details_changed BOOLEAN := FALSE;
BEGIN
    IF INSERTING THEN
        IF :NEW.process_status <> 'APPLY' OR :NEW.approved_amount IS NOT NULL THEN
            v_audit_result := 'ABNORMAL';
        END IF;

        v_note := SUBSTR(
            'AUTO_REFUND: INSERT status=' || :NEW.process_status ||
            ', requested=' || TO_CHAR(:NEW.refund_amount),
            1, 300);
    ELSE
        v_details_changed :=
               :OLD.payment_id <> :NEW.payment_id
            OR :OLD.refund_amount <> :NEW.refund_amount
            OR :OLD.refund_reason <> :NEW.refund_reason
            OR NVL(:OLD.approved_amount, -1) <> NVL(:NEW.approved_amount, -1);

        IF NOT v_details_changed AND :OLD.process_status = :NEW.process_status THEN
            RETURN;
        END IF;

        IF :OLD.payment_id <> :NEW.payment_id THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :OLD.process_status <> :NEW.process_status
              AND NOT (
                  (:OLD.process_status = 'APPLY' AND :NEW.process_status IN ('APPROVED', 'REJECTED'))
                  OR (:OLD.process_status = 'APPROVED' AND :NEW.process_status = 'DONE')
              ) THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :OLD.process_status IN ('APPROVED', 'REJECTED', 'DONE')
              AND v_details_changed THEN
            v_audit_result := 'ABNORMAL';
        END IF;

        IF :NEW.process_status = 'APPLY' AND :NEW.approved_amount IS NOT NULL THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :NEW.process_status = 'APPROVED'
              AND (:NEW.approved_amount IS NULL
                   OR :NEW.approved_amount < 0
                   OR :NEW.approved_amount > :NEW.refund_amount) THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :NEW.process_status = 'REJECTED'
              AND NVL(:NEW.approved_amount, -1) <> 0 THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :NEW.process_status = 'DONE'
              AND (:NEW.approved_amount IS NULL OR :NEW.approved_amount < 0) THEN
            v_audit_result := 'ABNORMAL';
        END IF;

        v_note := SUBSTR(
            'AUTO_REFUND: UPDATE status=' || :OLD.process_status || ' -> ' ||
            :NEW.process_status ||
            CASE WHEN v_details_changed THEN ', critical fields changed' END,
            1, 300);
    END IF;

    INSERT INTO audit_logs (audit_object, audit_result, audited_at, exception_note)
    VALUES ('REFUND', v_audit_result, SYSDATE, v_note)
    RETURNING audit_id INTO v_audit_id;

    INSERT INTO audit_refund_checks (audit_id, refund_id)
    VALUES (v_audit_id, :NEW.refund_id);
END;
/

/* Type 2a: audit payment creation and changes to critical payment fields. */
CREATE OR REPLACE TRIGGER trg_payment_change_audit
AFTER INSERT OR UPDATE OF
    record_id,
    order_amount,
    pay_amount,
    pay_method,
    third_trade_no,
    pay_status
ON payments
FOR EACH ROW
DECLARE
    v_audit_id        audit_logs.audit_id%TYPE;
    v_audit_result    audit_logs.audit_result%TYPE := 'PASS';
    v_note            audit_logs.exception_note%TYPE;
    v_details_changed BOOLEAN := FALSE;
BEGIN
    IF INSERTING THEN
        IF :NEW.pay_status NOT IN ('UNPAID', 'PAID', 'FAILED') THEN
            v_audit_result := 'ABNORMAL';
        END IF;

        v_note := SUBSTR(
            'AUTO_PAYMENT: INSERT status=' || :NEW.pay_status ||
            ', order=' || TO_CHAR(:NEW.order_amount) ||
            ', paid=' || TO_CHAR(:NEW.pay_amount),
            1, 300);
    ELSE
        v_details_changed :=
               :OLD.record_id <> :NEW.record_id
            OR :OLD.order_amount <> :NEW.order_amount
            OR :OLD.pay_amount <> :NEW.pay_amount
            OR :OLD.pay_method <> :NEW.pay_method
            OR NVL(:OLD.third_trade_no, CHR(0)) <>
               NVL(:NEW.third_trade_no, CHR(0));

        IF NOT v_details_changed AND :OLD.pay_status = :NEW.pay_status THEN
            RETURN;
        END IF;

        IF :OLD.record_id <> :NEW.record_id THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :OLD.pay_status <> :NEW.pay_status
              AND NOT (
                  (:OLD.pay_status = 'UNPAID' AND :NEW.pay_status IN ('PAID', 'FAILED'))
                  OR (:OLD.pay_status = 'FAILED' AND :NEW.pay_status IN ('UNPAID', 'PAID'))
                  OR (:OLD.pay_status = 'PAID' AND :NEW.pay_status = 'REFUNDED')
              ) THEN
            v_audit_result := 'ABNORMAL';
        ELSIF :OLD.pay_status IN ('PAID', 'REFUNDED') AND v_details_changed THEN
            v_audit_result := 'ABNORMAL';
        END IF;

        v_note := SUBSTR(
            'AUTO_PAYMENT: UPDATE status=' || :OLD.pay_status || ' -> ' ||
            :NEW.pay_status ||
            CASE WHEN v_details_changed THEN ', critical fields changed' END,
            1, 300);
    END IF;

    INSERT INTO audit_logs (audit_object, audit_result, audited_at, exception_note)
    VALUES ('PAYMENT', v_audit_result, SYSDATE, v_note)
    RETURNING audit_id INTO v_audit_id;

    INSERT INTO audit_payment_checks (audit_id, payment_id)
    VALUES (v_audit_id, :NEW.payment_id);
END;
/

/* Deployment verification: all three rows must be VALID and USER_ERRORS empty. */
SELECT trigger_name, triggering_event, table_name, status
  FROM user_triggers
 WHERE trigger_name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
 )
 ORDER BY trigger_name;

SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'TRIGGER'
   AND name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
   )
 ORDER BY name, sequence;
