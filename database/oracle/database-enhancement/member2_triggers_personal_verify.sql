/*
  Read-only verification for the isolated APP2452098 trigger test.

  Connect to the APP2452098 DBeaver connection before running this file.
  It reads only M2_ test objects and never touches APPUSER business rows.
*/

SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS current_schema
  FROM dual;

SELECT trigger_name, triggering_event, table_name, status
  FROM user_triggers
 WHERE trigger_name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
 )
 ORDER BY trigger_name;

/* Expected: no rows. */
SELECT name, type, line, position, text
  FROM user_errors
 WHERE type = 'TRIGGER'
   AND name IN (
       'TRG_TASK_STATUS_AUDIT',
       'TRG_PAYMENT_CHANGE_AUDIT',
       'TRG_REFUND_CHANGE_AUDIT'
   )
 ORDER BY name, sequence;

SELECT audit_id,
       audit_object,
       audit_result,
       audited_at,
       exception_note
  FROM m2_audit_logs
 ORDER BY audit_id;

SELECT audit_object,
       audit_result,
       COUNT(*) AS audit_count
  FROM m2_audit_logs
 GROUP BY audit_object, audit_result
 ORDER BY audit_object, audit_result;

SELECT a.audit_id,
       a.audit_result,
       c.log_id,
       l.status_before,
       l.status_after
  FROM m2_audit_logs a
  JOIN m2_audit_status_checks c ON c.audit_id = a.audit_id
  JOIN m2_task_status_logs l ON l.log_id = c.log_id
 ORDER BY a.audit_id;

SELECT a.audit_id,
       a.audit_result,
       c.payment_id,
       p.pay_status,
       p.order_amount,
       p.pay_amount
  FROM m2_audit_logs a
  JOIN m2_audit_payment_checks c ON c.audit_id = a.audit_id
  JOIN m2_payments p ON p.payment_id = c.payment_id
 ORDER BY a.audit_id;

SELECT a.audit_id,
       a.audit_result,
       c.refund_id,
       r.process_status,
       r.refund_amount,
       r.approved_amount
  FROM m2_audit_logs a
  JOIN m2_audit_refund_checks c ON c.audit_id = a.audit_id
  JOIN m2_refunds r ON r.refund_id = c.refund_id
 ORDER BY a.audit_id;

/* Expected: 9 total audit rows and 9 total link rows. */
SELECT (SELECT COUNT(*) FROM m2_audit_logs) AS audit_count,
       (SELECT COUNT(*) FROM m2_audit_status_checks)
       + (SELECT COUNT(*) FROM m2_audit_payment_checks)
       + (SELECT COUNT(*) FROM m2_audit_refund_checks) AS link_count
  FROM dual;
