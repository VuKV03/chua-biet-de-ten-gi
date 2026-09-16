import React from 'react'
import TaskEmptyState from './TaskEmptyState';
import TaskCard from './TaskCard';

const TaskList = ({ listTasks, filter, handleTaskChanged }: { listTasks: any[], filter: string, handleTaskChanged: () => void }) => {

  if (!listTasks || listTasks.length === 0) {
    return <TaskEmptyState filter={filter} />
  }
  return (
    <div className="space-y-3">
      {listTasks.map((task, index) => (
        <TaskCard
          key={task._id ?? index}
          task={task}
          index={index}
          handleTaskChanged={handleTaskChanged}
        />
      ))}
    </div>
  )
}

export default TaskList