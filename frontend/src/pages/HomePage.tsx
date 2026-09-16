
import AddTask from '../features/task/components/AddTask'
import Footer from '../features/task/components/Footer'
import Header from '../features/task/components/Header'
import StatsAndFilters from '../features/task/components/StatsAndFilter'
import TaskList from '../features/task/components/TaskList'
import TaskListPagination from '../features/task/components/TaskListPagination'
import DateTimeFilter from '../features/task/components/DateTimeFilter'
import { useEffect, useState } from 'react'
import axios from 'axios'
import { toast, Toaster } from 'sonner'
import axiosInstance from '@/shared/lib/axios'


const HomePage = () => {
  const [taskBuffer, setTaskBuffer] = useState([]);
  const [activeTaskCount, setActiveTaskCount] = useState(0);
  const [completedTaskCount, setCompletedTaskCount] = useState(0);
  const [filter, setFilter] = useState("all");


  const fetchTasks = async () => {
    try {
      const response = await axiosInstance.get('/todo/get-all');
      setTaskBuffer(response.data.items);
      setActiveTaskCount(response.data.activeCount);
      setCompletedTaskCount(response.data.completedCount);

    } catch (error) {
      console.log("Lỗi xảy ra khi lấy danh sách nhiệm vụ!", error);
      toast.error("Không thể lấy danh sách nhiệm vụ");
    }
  }



  const handleTaskChanged = () => {
    fetchTasks();
  }

  useEffect(() => {
    fetchTasks();
  }, []);


  const filteredTasks = taskBuffer.filter((task) => {
    switch (filter) {
      case "all":
        return true;
      case "active":
        return task.status === 1;
      case "completed":
        return task.status === 2;
    }
  });

  return (
    <>
      <div className="min-h-screen w-full bg-white relative">
        {/* Pink Glow Background */}
        <div
          className="absolute inset-0 z-0"
          style={{
            backgroundImage: `
        radial-gradient(125% 125% at 50% 90%, #ffffff 40%, #ec4899 100%)
      `,
            backgroundSize: "100% 100%",
          }}
        />
        <div className="container pt-8 mx-auto relative z-10 ">
          <div className="w-full max-w-2xl p-6 mx-auto space-y-6">
            {/* Đầu trang */}
            <Header />

            {/* Tạo nhiệm vụ */}
            <AddTask handleNewTaskAdded={handleTaskChanged} />

            {/* Thống kê và bộ lọc */}
            <StatsAndFilters
              filter={filter}
              setFilter={setFilter}
              activeTaskCount={activeTaskCount}
              completedTaskCount={completedTaskCount}
            />

            {/* Danh sách nhiệm vụ */}
            <TaskList listTasks={filteredTasks} filter={filter} handleTaskChanged={handleTaskChanged} />

            {/* Phân trang và lọc theo date */}
            <div className="flex flex-col items-center justify-between gap-6 sm:flex-row">
              <TaskListPagination />
              <DateTimeFilter />
            </div>

            {/* Chân trang */}
            <Footer
              activeTasksCount={activeTaskCount}
              completedTasksCount={completedTaskCount}
            />

            <Toaster position="bottom-right" />

          </div>
        </div>
      </div>

    </>
  )
}

export default HomePage
