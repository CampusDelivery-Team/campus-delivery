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

-- 【调用示例】(已注释，仅作文档参考。真实测试代码见 05_test.sql)
-- DECLARE
--     v_msg VARCHAR2(200);
-- BEGIN
--     sp_audit_runner(485, 'APPROVED', v_msg);
--     DBMS_OUTPUT.PUT_LINE('审核测试结果: ' || v_msg);
-- END;
-- /