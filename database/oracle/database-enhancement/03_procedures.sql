-- =============================================
-- 过程名称: sp_manage_account_status
-- 功能描述: 用户封禁/解封过程。封禁时同步将跑腿员状态置为下线。
-- 输入参数: p_user_id (用户编号), p_action (操作类型: BLOCK 封禁, UNBLOCK 解封)
-- 输出参数: p_result (执行结果及提示信息)
-- 异常情况: 找不到用户报 NO_DATA_FOUND，其他数据库层面错误触发 OTHERS 异常并整体回滚。
-- =============================================
CREATE OR REPLACE PROCEDURE sp_manage_account_status (
    p_user_id IN NUMBER,
    p_action IN VARCHAR2,
    p_result OUT VARCHAR2
) AS
    v_current_status VARCHAR2(20);
    v_role VARCHAR2(20);
BEGIN
    -- 1. 查询用户当前状态和角色
    SELECT account_status, user_role INTO v_current_status, v_role
    FROM APPUSER.users
    WHERE user_id = p_user_id;

    -- 2. 权限校验：只允许操作普通用户和跑腿员
    IF v_role NOT IN ('USER', 'RUNNER') THEN
        p_result := 'FAILED: 只能封禁或解封普通用户与跑腿员';
        RETURN;
    END IF;

    -- 3. 执行封禁逻辑
    IF p_action = 'BLOCK' THEN
        IF v_current_status = 'BLOCKED' THEN
            p_result := 'FAILED: 账号已被封禁，请勿重复操作';
            RETURN;
        END IF;

        UPDATE APPUSER.users
        SET account_status = 'BLOCKED'
        WHERE user_id = p_user_id;

        -- 联动：如果是跑腿员，同时强制离线
        UPDATE APPUSER.runners
        SET work_status = 'OFFLINE'
        WHERE user_id = p_user_id;

        p_result := 'SUCCESS: 账号封禁成功';

    -- 4. 执行解封逻辑
    ELSIF p_action = 'UNBLOCK' THEN
        IF v_current_status = 'NORMAL' THEN
            p_result := 'FAILED: 账号状态正常，无需解封';
            RETURN;
        END IF;

        UPDATE APPUSER.users
        SET account_status = 'NORMAL'
        WHERE user_id = p_user_id;

        p_result := 'SUCCESS: 账号解封成功';
    ELSE
        p_result := 'FAILED: 未知的操作类型(仅支持 BLOCK 或 UNBLOCK)';
    END IF;

    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'FAILED: 用户不存在';
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'FAILED: 系统异常 - ' || SQLERRM;
END sp_manage_account_status;
/

-- 【调用示例】(已注释，仅作文档参考。真实测试代码见 05_test.sql)
-- SET SERVEROUTPUT ON;
-- DECLARE
--     v_msg VARCHAR2(200);
-- BEGIN
--     sp_manage_account_status(41, 'BLOCK', v_msg);
--     DBMS_OUTPUT.PUT_LINE('封禁测试结果: ' || v_msg);
-- END;
-- /


-- =============================================
-- 过程名称: sp_set_default_address
-- 功能描述: 设置用户的默认收货地址。同一用户最多只能有一个默认地址。
-- 输入参数: p_user_id (用户编号), p_address_no (地址序号)
-- 输出参数: p_result (执行结果及提示信息)
-- 异常情况: 地址不存在或不属于该用户则返回拦截提示；数据库写入异常时触发整体回滚。
-- =============================================
CREATE OR REPLACE PROCEDURE sp_set_default_address (
    p_user_id IN NUMBER,
    p_address_no IN NUMBER,
    p_result OUT VARCHAR2
) AS
    v_count NUMBER;
BEGIN
    -- 1. 检查该地址是否存在且属于该用户
    SELECT COUNT(*) INTO v_count
    FROM APPUSER.user_addresses
    WHERE user_id = p_user_id AND address_no = p_address_no;

    IF v_count = 0 THEN
        p_result := 'FAILED: 该地址不存在或不属于当前用户';
        RETURN;
    END IF;

    -- 2. 在同一事务内：先将该用户的所有地址设为非默认
    UPDATE APPUSER.user_addresses
    SET is_default = 'N'
    WHERE user_id = p_user_id;

    -- 3. 将指定的地址设为默认
    UPDATE APPUSER.user_addresses
    SET is_default = 'Y'
    WHERE user_id = p_user_id AND address_no = p_address_no;

    -- 4. 提交事务，保证一致性
    COMMIT;
    p_result := 'SUCCESS: 默认地址设置成功';

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'FAILED: 系统异常 - ' || SQLERRM;
END sp_set_default_address;
/

-- 【调用示例】(已注释，仅作文档参考。真实测试代码见 05_test.sql)
-- DECLARE
--     v_msg VARCHAR2(200);
-- BEGIN
--     sp_set_default_address(41, 1, v_msg);
--     DBMS_OUTPUT.PUT_LINE('设置默认地址结果: ' || v_msg);
-- END;
-- /


-- =============================================
-- 过程名称: sp_audit_runner
-- 功能描述: 配送员审核过程。审核通过时同步更新用户角色并初始化工作状态。
-- 输入参数: p_user_id (用户编号), p_audit_status (审核结果: APPROVED 通过, REJECTED 驳回)
-- 输出参数: p_result (执行结果及提示信息)
-- 异常情况: 找不到申请记录报 NO_DATA_FOUND，其他更新异常触发整体事务回滚。
-- =============================================
CREATE OR REPLACE PROCEDURE sp_audit_runner (
    p_user_id IN NUMBER,
    p_audit_status IN VARCHAR2,
    p_result OUT VARCHAR2
) AS
    v_current_audit VARCHAR2(20);
BEGIN
    -- 1. 查询跑腿员当前审核状态
    SELECT audit_status INTO v_current_audit
    FROM APPUSER.runners
    WHERE user_id = p_user_id;

    -- 2. 状态校验：只有待审核(PENDING)状态才能进行操作
    IF v_current_audit <> 'PENDING' THEN
        p_result := 'FAILED: 只能审核状态为 PENDING 的申请';
        RETURN;
    END IF;

    -- 3. 审核通过逻辑
    IF p_audit_status = 'APPROVED' THEN
        -- 更新跑腿员表
        UPDATE APPUSER.runners
        SET audit_status = 'APPROVED',
            work_status = 'FREE'
        WHERE user_id = p_user_id;

        -- 联动更新用户表角色
        UPDATE APPUSER.users
        SET user_role = 'RUNNER'
        WHERE user_id = p_user_id;

        p_result := 'SUCCESS: 审核通过，已授予跑腿员角色';

    -- 4. 审核驳回逻辑
    ELSIF p_audit_status = 'REJECTED' THEN
        UPDATE APPUSER.runners
        SET audit_status = 'REJECTED'
        WHERE user_id = p_user_id;

        p_result := 'SUCCESS: 审核已驳回';
    ELSE
        p_result := 'FAILED: 未知的审核状态(仅支持 APPROVED 或 REJECTED)';
    END IF;

    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 'FAILED: 找不到该用户的跑腿员申请记录';
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 'FAILED: 系统异常 - ' || SQLERRM;
END sp_audit_runner;
/

-- =============================================
-- 过程名称: sp_accept_task_atomic
-- 功能描述: 原子完成跑腿员抢单或管理员派单。过程内部不提交或回滚，
--           事务边界由调用方控制。
-- 输入参数: 任务、跑腿员、操作者和操作类型（SELF/ADMIN）
-- 输出参数: 结果代码和成功时生成的接派记录编号
-- =============================================
CREATE OR REPLACE PROCEDURE sp_accept_task_atomic (
    p_task_id          IN tasks.task_id%TYPE,
    p_runner_id        IN runners.runner_id%TYPE,
    p_operator_user_id IN users.user_id%TYPE,
    p_operation_type   IN assign_records.operation_type%TYPE,
    p_result_code      OUT VARCHAR2,
    p_record_id        OUT assign_records.record_id%TYPE
) AS
    v_task_status       tasks.task_status%TYPE;
    v_publisher_user_id tasks.publisher_user_id%TYPE;
    v_runner_user_id    runners.user_id%TYPE;
    v_runner_role       users.user_role%TYPE;
    v_account_status    users.account_status%TYPE;
    v_audit_status      runners.audit_status%TYPE;
    v_work_status       runners.work_status%TYPE;
    v_operator_count    PLS_INTEGER;
BEGIN
    p_result_code := 'FAILED';
    p_record_id := NULL;

    IF p_operation_type IS NULL OR p_operation_type NOT IN ('SELF', 'ADMIN') THEN
        p_result_code := 'INVALID_OPERATION';
        RETURN;
    END IF;

    BEGIN
        SELECT task_status, publisher_user_id
          INTO v_task_status, v_publisher_user_id
          FROM tasks
         WHERE task_id = p_task_id
         FOR UPDATE;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result_code := 'TASK_NOT_FOUND';
            RETURN;
    END;

    IF v_task_status <> 'WAITING' THEN
        p_result_code := 'TASK_NOT_WAITING';
        RETURN;
    END IF;

    BEGIN
        SELECT r.user_id,
               u.user_role,
               u.account_status,
               r.audit_status,
               r.work_status
          INTO v_runner_user_id,
               v_runner_role,
               v_account_status,
               v_audit_status,
               v_work_status
          FROM runners r
          JOIN users u ON u.user_id = r.user_id
         WHERE r.runner_id = p_runner_id
         FOR UPDATE OF r.work_status, u.account_status;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            p_result_code := 'RUNNER_NOT_FOUND';
            RETURN;
    END;

    IF v_runner_role <> 'RUNNER'
       OR v_account_status <> 'NORMAL'
       OR v_audit_status <> 'APPROVED'
       OR v_work_status NOT IN ('FREE', 'BUSY') THEN
        p_result_code := 'RUNNER_INELIGIBLE';
        RETURN;
    END IF;

    IF v_publisher_user_id = v_runner_user_id THEN
        p_result_code := 'PUBLISHER_CANNOT_ACCEPT';
        RETURN;
    END IF;

    IF p_operation_type = 'SELF' THEN
        IF p_operator_user_id IS NULL OR p_operator_user_id <> v_runner_user_id THEN
            p_result_code := 'SELF_OPERATOR_MISMATCH';
            RETURN;
        END IF;
    ELSE
        SELECT COUNT(*)
          INTO v_operator_count
          FROM users
         WHERE user_id = p_operator_user_id
           AND user_role = 'ADMIN'
           AND account_status = 'NORMAL';

        IF v_operator_count <> 1 THEN
            p_result_code := 'ADMIN_OPERATOR_INVALID';
            RETURN;
        END IF;
    END IF;

    UPDATE tasks
       SET task_status = 'ASSIGNED'
     WHERE task_id = p_task_id
       AND task_status = 'WAITING';

    IF SQL%ROWCOUNT <> 1 THEN
        p_result_code := 'TASK_NOT_WAITING';
        RETURN;
    END IF;

    UPDATE runners
       SET work_status = 'BUSY'
     WHERE runner_id = p_runner_id;

    INSERT INTO assign_records (
        task_id,
        runner_id,
        operation_type,
        assigned_at,
        reassign_reason
    ) VALUES (
        p_task_id,
        p_runner_id,
        p_operation_type,
        SYSDATE,
        NULL
    )
    RETURNING record_id INTO p_record_id;

    INSERT INTO task_status_logs (
        record_id,
        status_before,
        status_after,
        operator_user_id,
        operated_at
    ) VALUES (
        p_record_id,
        'WAITING',
        'ASSIGNED',
        p_operator_user_id,
        SYSDATE
    );

    p_result_code := 'SUCCESS';
END sp_accept_task_atomic;
/

-- 【调用示例】(已注释，仅作文档参考。真实测试代码见 05_test.sql)
-- DECLARE
--     v_msg VARCHAR2(200);
-- BEGIN
--     sp_audit_runner(485, 'APPROVED', v_msg);
--     DBMS_OUTPUT.PUT_LINE('审核测试结果: ' || v_msg);
-- END;
-- /
