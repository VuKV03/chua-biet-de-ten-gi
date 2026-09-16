import { Card } from "@/features/ui/card"
import type { Task } from "../types/task.types"
import { cn } from "@/lib/utils"
import { Button } from "@/features/ui/button"
import { Calendar, CheckCircle2, Circle, SquarePen, Trash2 } from "lucide-react"
import { Input } from "@/features/ui/input"
import { toast } from "sonner"
import axiosInstance from "@/shared/lib/axios"
import { useState } from "react"

interface TaskCardProps {
  task: Task,
  index: number,
  handleTaskChanged: () => void
}


const TaskCard = ({ task, index, handleTaskChanged }: TaskCardProps) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedTitle, setEditedTitle] = useState(task.title);

  const deleteTask = async (id: string) => {
    try {
      await axiosInstance.delete(`/todo/delete/${id}`);
      toast.success("Nhiệm vụ đã được xóa");
      handleTaskChanged();
    } catch (error) {
      console.log("Lỗi xảy ra khi xóa nhiệm vụ!", error);
      toast.error("Không thể xóa nhiệm vụ");
    }
  }

  const updateTask = async (id: string) => {
    try {
      setIsEditing(false);
      await axiosInstance.put(`/todo/update`, {
        id: id,
        title: editedTitle

      });
      toast.success("Nhiệm vụ đã được cập nhật");
      handleTaskChanged();
    } catch (error) {
      console.log("Lỗi xảy ra khi cập nhật nhiệm vụ!", error);
      toast.error("Không thể cập nhật nhiệm vụ");
    }
  }

  const isCompleted = task.status === 2 || task.status === "complete";

  const toggleTaskCompleteButton = async () => {
    try {
      if (!isCompleted) {
        await axiosInstance.put(`/todo/update`, {
          id: task.id,
          status: 2,
          completedAt: new Date().toISOString(),
        });
        toast.success(`${task.title} đã hoàn thành!`);
      } else {
        await axiosInstance.put(`/todo/update`, {
          id: task.id,
          status: 1,
          completedAt: null,
        });
        toast.success(`${task.title} đã đổi sang chưa hoàn thành!`);
      }

      handleTaskChanged();
    } catch (e) {
      console.error("Lỗi xảy ra khi update task!", e);
      toast.error("Lỗi xảy ra khi cập nhật nhiệm vụ!");
    }
  };

  return (
    <>
      <Card
        className={cn(
          "p-4 bg-gradient-card border-0 shadow-custom-md hover:shadow-custom-lg transition duration-200 animate-fade-in group",
          isCompleted && "opacity-70"
        )}
        style={{ animationDelay: `${index * 50}ms` }}
      >
        <div className="flex items-center gap-4">
          {/* Nút tròn */}
          <Button
            variant="ghost"
            size="icon"
            className={cn(
              "flex-shrink-0 size-8 rounded-full transition-all duration-200",
              isCompleted
                ? "text-success hover:text-success/80"
                : "text-muted-foreground hover:text-primary"
            )}
            onClick={toggleTaskCompleteButton}
          >
            {isCompleted ? (
              <CheckCircle2 className="size-5" />
            ) : (
              <Circle className="size-5" />
            )}
          </Button>

          {/* Hiển thị hoặc chỉnh sửa tiêu đề */}
          <div className="flex-1 min-w-0">
            {isEditing ? (
              <Input
                placeholder="Cần phải làm gì?"
                className="flex-1 h-12 text-base border-border/50 focus:border-primary/50 focus:ring-primary/20"
                type="text"
                value={editedTitle}
                onChange={(e) => setEditedTitle(e.target.value)}
                onKeyPress={(e) => {
                  if (e.key === "Enter") {
                    if (task.id) updateTask(task.id);
                    setIsEditing(false);
                  }
                }}
                onBlur={() => {
                  setIsEditing(false);
                  setEditedTitle(task.title || "");
                }}
              />
            ) : (
              <p
                className={cn(
                  "text-base transition-all duration-200",
                  isCompleted
                    ? "line-through text-muted-foreground"
                    : "text-foreground"
                )}
              >
                {task.title}
              </p>
            )}
          </div>

          {/* Ngày tạo & ngày hoàn thành */}
          <div className="flex items-center gap-2 mt-1">
            <Calendar className="size-3 text-muted-foreground" />
            <span className="text-xs text-muted-foreground">
              {task.createdAt
                ? new Date(task.createdAt).toLocaleDateString("vi-VN")
                : ""}
            </span>
            {task.completedAt && (
              <>
                <span className="text-xs text-muted-foreground"> - </span>
                <Calendar className="size-3 text-muted-foreground" />
                <span className="text-xs text-muted-foreground">
                  {new Date(task.completedAt).toLocaleDateString("vi-VN")}
                </span>
              </>
            )}
          </div>

          {/* Nút chỉnh & xóa */}
          <div className="hidden gap-2 group-hover:inline-flex animate-slide-up">
            {/* Nút edit */}
            <Button
              variant="ghost"
              size="icon"
              className="flex-shrink-0 transition-colors size-8 text-muted-foreground hover:text-info"
              onClick={() => {
                setIsEditing(true);
                setEditedTitle(task.title || "");
              }}
            >
              <SquarePen className="size-4" />
            </Button>

            {/* Nút xóa */}
            <Button
              variant="ghost"
              size="icon"
              className="flex-shrink-0 transition-colors size-8 text-muted-foreground hover:text-destructive"
              onClick={() => {
                if (task.id) deleteTask(task.id);
              }}
            >
              <Trash2 className="size-4" />
            </Button>
          </div>
        </div>
      </Card>
    </>
  );
};

export default TaskCard;