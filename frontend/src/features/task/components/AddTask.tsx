import React, { useState } from 'react'
import { Card } from '../../ui/card'
import { Input } from '../../ui/input'
import { Button } from '../../ui/button'
import { Plus } from 'lucide-react'
import axios from 'axios'
import { toast } from 'sonner'
import axiosInstance from '@/shared/lib/axios'

const AddTask = ({ handleNewTaskAdded }: { handleNewTaskAdded: () => void }) => {
  const [newTaskTitle, setNewTaskTitle] = useState('');
  const addNewTask = async () => {
    if (newTaskTitle.trim()) {
      try {
        await axiosInstance.post('/todo/create', {
          title: newTaskTitle

        })
        toast.success(`Nhiệm vụ mới "${newTaskTitle}"`)
        handleNewTaskAdded();
      } catch (error) {
        console.log("Lỗi xảy ra khi thêm task!", error);
        toast.error("Không thể thêm nhiệm vụ vào lúc này.");
      }
      setNewTaskTitle("");
    } else {
      toast.error("Vui lòng nhập tên nhiệm vụ!");
    }
  }

  return (
    <>
      <Card className="p-6 border-0 bg-gradient-card shadow-custom-lg">
        <div className="flex flex-col gap-3 sm:flex-row">
          <Input
            type="text"
            placeholder='Thêm nhiệm vụ mới...'
            className="h-12 text-base bg-slate-50 sm:flex-1 border-border/50 focus:border-primary/50 focus:ring-primary/50"
            value={newTaskTitle}
            onChange={(e) => setNewTaskTitle(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                addNewTask();
              }
            }}
          />

          <Button
            variant="gradient"
            size="xl"
            className="px-6"
            onClick={addNewTask}
            disabled={newTaskTitle.trim() === ''}
          >
            <Plus className="size-5" />
            Thêm
          </Button>
        </div>
      </Card>
    </>
  )
}

export default AddTask