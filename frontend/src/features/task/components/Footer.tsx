import React, { useEffect, useState } from 'react'
import axios from 'axios'

const Footer = ({ completedTasksCount = 0, activeTasksCount = 0 }) => {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)


  return (
    <>
      {completedTasksCount + activeTasksCount > 0 && (
        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            {
              completedTasksCount > 0 && (
                <>
                  Tuyệt vời! Đã hoàn thành {completedTasksCount} việc
                  {
                    activeTasksCount > 0 && `, còn ${activeTasksCount} việc cần làm nữa thôi!`
                  }
                </>
              )
            }
            {
              completedTasksCount === 0 && activeTasksCount > 0 && (
                <>
                  Hãy bắt đầu làm {activeTasksCount} nhiệm vụ nào!
                </>
              )
            }

          </p>

        </div>
      )}
    </>
  )
}

export default Footer