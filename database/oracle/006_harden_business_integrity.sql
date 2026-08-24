SET SERVEROUTPUT ON;

DECLARE
    duplicate_count NUMBER;
BEGIN
    SELECT COUNT(*)
      INTO duplicate_count
      FROM (
          SELECT user_id
            FROM APPUSER.user_addresses
           WHERE is_default = 'Y'
           GROUP BY user_id
          HAVING COUNT(*) > 1
      );

    IF duplicate_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20031,
            '存在同一用户的多条默认地址，请先修复数据后再执行 006_harden_business_integrity.sql。');
    END IF;
END;
/

DECLARE
    duplicate_count NUMBER;
BEGIN
    SELECT COUNT(*)
      INTO duplicate_count
      FROM (
          SELECT UPPER(TRIM(service_name)) AS normalized_name
            FROM APPUSER.service_types
           GROUP BY UPPER(TRIM(service_name))
          HAVING COUNT(*) > 1
      );

    IF duplicate_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20032,
            '存在忽略大小写或首尾空格后重名的服务类型，请先修复数据后再执行 006_harden_business_integrity.sql。');
    END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE q'[
        CREATE UNIQUE INDEX APPUSER.uk_user_addresses_one_default
            ON APPUSER.user_addresses (
                CASE WHEN is_default = 'Y' THEN user_id END
            )
    ]';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE NOT IN (-955, -1408) THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE q'[
        CREATE UNIQUE INDEX APPUSER.uk_service_types_name_ci
            ON APPUSER.service_types (UPPER(TRIM(service_name)))
    ]';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE NOT IN (-955, -1408) THEN
            RAISE;
        END IF;
END;
/

COMMIT;

SELECT index_name, uniqueness
  FROM user_indexes
 WHERE index_name IN ('UK_USER_ADDRESSES_ONE_DEFAULT', 'UK_SERVICE_TYPES_NAME_CI')
 ORDER BY index_name;
