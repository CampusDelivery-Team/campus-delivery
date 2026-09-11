/*
  Roll back only the member 2 trigger objects.
  Existing business rows and audit rows are intentionally preserved.
*/

BEGIN
    EXECUTE IMMEDIATE 'DROP TRIGGER trg_task_status_audit';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4080 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TRIGGER trg_payment_change_audit';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4080 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TRIGGER trg_refund_change_audit';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -4080 THEN RAISE; END IF;
END;
/
