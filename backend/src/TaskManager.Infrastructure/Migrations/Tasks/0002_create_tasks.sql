CREATE TABLE tasks.tasks (
    id uuid PRIMARY KEY,
    owner_id uuid NOT NULL,
    title varchar(120) NOT NULL,
    description text,
    status text NOT NULL CHECK (status IN ('open', 'completed')),
    due_date date
);

CREATE INDEX tasks_owner_title_id_idx ON tasks.tasks (owner_id, title, id);
