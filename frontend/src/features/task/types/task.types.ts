export interface Task {
  id?: string,
  title: string,
  status: string | number,
  completedAt: Date | null,
  createdAt: Date | null,
}