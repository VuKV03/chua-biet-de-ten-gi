import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/features/ui/button"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/features/ui/popover"
import { options } from "@/lib/data"

interface DateTimeFilterProps {
  dateFilter?: string
  setDateFilter?: (value: string) => void
}

const DateTimeFilter = ({ dateFilter = "all", setDateFilter }: DateTimeFilterProps) => {
  const [open, setOpen] = React.useState(false)
  const [value, setValue] = React.useState(dateFilter)

  const selectedOption = options.find((opt) => opt.value === value)

  const handleSelect = (val: string) => {
    setValue(val)
    setDateFilter?.(val)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger>
        <Button
          variant="outline"
          size="default"
          role="combobox"
          aria-expanded={open}
          className="w-[140px] justify-between text-xs sm:text-sm h-9 bg-background border-border/60 hover:bg-accent"
        >
          {selectedOption ? selectedOption.label : "Chọn thời gian"}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[140px] p-1 bg-popover text-popover-foreground rounded-lg shadow-lg border border-border">
        <div className="flex flex-col gap-0.5">
          {options.map((option) => {
            const isSelected = value === option.value
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => handleSelect(option.value)}
                className={cn(
                  "flex items-center justify-between px-2.5 py-1.5 text-xs sm:text-sm rounded-md hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer text-left w-full",
                  isSelected && "bg-accent/80 font-medium text-primary"
                )}
              >
                <span>{option.label}</span>
                {isSelected && <Check className="h-3.5 w-3.5 text-primary" />}
              </button>
            )
          })}
        </div>
      </PopoverContent>
    </Popover>
  )
}

export default DateTimeFilter