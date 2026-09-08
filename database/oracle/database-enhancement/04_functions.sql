/*
  组员4数据库增强：业务函数与信誉分规则。

  基础表结构和基础数据准备完成后，由APPUSER执行一次本脚本。
  下列函数为SQL、视图和存储过程提供可复用的计算与资格判断能力，
  不能替代C#服务层中的身份授权、事务、行锁和业务写入逻辑。

  脚本最后会把历史上超过100的信誉分统一截断为100，并将
  CK_RUNNERS_CREDIT替换为0至100的检查约束。由于没有保留原始超额值，
  该数据归一化操作无法反向恢复。
*/

CREATE OR REPLACE FUNCTION fn_calculate_task_price (
    p_service_type_id IN service_types.service_type_id%TYPE,
    p_extra_amount    IN NUMBER DEFAULT 0
) RETURN NUMBER
IS
    v_base_price       service_types.base_price%TYPE;
    v_calculated_price NUMBER;
BEGIN
    IF p_service_type_id IS NULL THEN
        RAISE_APPLICATION_ERROR(-20041, 'Service type id is required.');
    END IF;

    IF p_extra_amount IS NULL OR p_extra_amount < 0 THEN
        RAISE_APPLICATION_ERROR(-20042, 'Extra amount must be zero or greater.');
    END IF;

    SELECT base_price
      INTO v_base_price
      FROM service_types
     WHERE service_type_id = p_service_type_id
       AND type_status = 'ENABLED';

    -- 服务类型表中的基础价格是唯一数据来源，调用方只传入明确的
    -- 距离、加急、重量或复杂度附加费。
    v_calculated_price := ROUND(v_base_price + p_extra_amount, 2);

    IF v_calculated_price > 99999999.99 THEN
        RAISE_APPLICATION_ERROR(-20045, 'Calculated task price exceeds NUMBER(10,2).');
    END IF;

    RETURN v_calculated_price;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20043, 'Enabled service type was not found.');
END;
/

CREATE OR REPLACE FUNCTION fn_runner_can_accept_task (
    p_runner_id IN runners.runner_id%TYPE,
    p_task_id   IN tasks.task_id%TYPE
) RETURN NUMBER
IS
    v_match_count PLS_INTEGER;
BEGIN
    IF p_runner_id IS NULL OR p_task_id IS NULL THEN
        RETURN 0;
    END IF;

    SELECT COUNT(*)
      INTO v_match_count
      FROM runners r
      JOIN users u
        ON u.user_id = r.user_id
      JOIN tasks t
        ON t.task_id = p_task_id
     WHERE r.runner_id = p_runner_id
       AND u.user_role = 'RUNNER'
       AND u.account_status = 'NORMAL'
       AND r.audit_status = 'APPROVED'
       AND r.work_status IN ('FREE', 'BUSY')
       AND t.task_status = 'WAITING'
       AND t.publisher_user_id <> r.user_id;

    RETURN CASE WHEN v_match_count = 1 THEN 1 ELSE 0 END;
END;
/

CREATE OR REPLACE FUNCTION fn_get_credit_level (
    p_credit_score IN runners.credit_score%TYPE
) RETURN VARCHAR2 DETERMINISTIC
IS
BEGIN
    IF p_credit_score IS NULL OR p_credit_score < 0 OR p_credit_score > 100 THEN
        RAISE_APPLICATION_ERROR(-20044, 'Credit score must be between zero and 100.');
    END IF;

    RETURN CASE
        WHEN p_credit_score >= 90 THEN 'EXCELLENT'
        WHEN p_credit_score >= 80 THEN 'GOOD'
        WHEN p_credit_score >= 70 THEN 'NORMAL'
        WHEN p_credit_score >= 60 THEN 'WATCH'
        ELSE 'RISK'
    END;
END;
/

/*
  收紧信誉分约束前先归一化已有数据。
  部署前必须备份信誉分超过100的记录，并暂停评价和投诉相关写入。
*/
UPDATE runners
   SET credit_score = 100
 WHERE credit_score > 100;

COMMIT;

ALTER TABLE runners DROP CONSTRAINT ck_runners_credit;

ALTER TABLE runners ADD CONSTRAINT ck_runners_credit
    CHECK (credit_score BETWEEN 0 AND 100);

COMMENT ON COLUMN runners.credit_score IS
    '信誉分，范围0至100，默认100';

SELECT 'Member 4 functions and credit-score rules created. Run 05_test.sql next.'
       AS deployment_result
  FROM dual;
