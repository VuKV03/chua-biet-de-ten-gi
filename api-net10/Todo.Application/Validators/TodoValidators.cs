using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Todo.Application.Commands;

namespace Todo.Application.Validators
{
    public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
    {
        public CreateTodoCommandValidator() {
            RuleFor(x => x.title)
                .NotEmpty().WithMessage("Tiêu đề công việc không được để trống!")
                .MaximumLength(500).WithMessage("Tiêu đề công việc tối đa 500 ký tự!");
        }
    }
}
