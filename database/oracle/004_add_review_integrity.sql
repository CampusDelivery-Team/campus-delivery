/*
  Review integrity migration.
  Run once on an existing database after 003_add_account_lifecycle.sql.

  This migration adds reviews.task_id so Oracle can enforce one review per task,
  while the composite foreign key keeps the review bound to the actual assignment
  record of that same task.
*/

DECLARE
    duplicate_task_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO duplicate_task_count
    FROM (
        SELECT ar.task_id
        FROM reviews rv
        JOIN assign_records ar ON ar.record_id = rv.record_id
        GROUP BY ar.task_id
        HAVING COUNT(*) > 1
    );

    IF duplicate_task_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20041,
            'Review migration stopped: existing data contains multiple reviews for one task.'
        );
    END IF;
END;
/

ALTER TABLE assign_records ADD CONSTRAINT uk_assign_record_task
    UNIQUE (record_id, task_id);

ALTER TABLE reviews ADD task_id NUMBER;

UPDATE reviews rv
SET task_id = (
    SELECT ar.task_id
    FROM assign_records ar
    WHERE ar.record_id = rv.record_id
);

ALTER TABLE reviews MODIFY (task_id NOT NULL);

ALTER TABLE reviews DROP CONSTRAINT uk_reviews_record;
ALTER TABLE reviews DROP CONSTRAINT fk_reviews_record;

ALTER TABLE reviews ADD CONSTRAINT uk_reviews_task
    UNIQUE (task_id);

ALTER TABLE reviews ADD CONSTRAINT fk_reviews_record_task
    FOREIGN KEY (record_id, task_id)
    REFERENCES assign_records(record_id, task_id);

CREATE INDEX idx_reviews_record ON reviews(record_id);

COMMENT ON COLUMN reviews.task_id IS '任务编号，唯一，一项任务最多一条评价';
COMMENT ON COLUMN reviews.record_id IS '任务最终有效接派记录编号，与 task_id 组成复合外键';
COMMENT ON COLUMN reviews.credit_delta IS '系统按评分计算后实际生效的信誉变动值';

COMMIT;
