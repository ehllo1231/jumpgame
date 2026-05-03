# AGENTS.md

## Development Guidelines

When working on this project, always prioritize clean, maintainable, and extensible software design.

### Core Principles

- Follow object-oriented programming principles.
- Follow the SOLID principles.
- Keep the codebase easy to extend, modify, test, and maintain.
- Prefer clear structure over quick, hard-coded solutions.
- Design systems so that new features can be added with minimal changes to existing code.

### Object-Oriented Design

- Give each class a clear responsibility.
- Keep classes small and focused.
- Use meaningful names for classes, methods, variables, and files.
- Avoid mixing unrelated responsibilities in a single class.
- Prefer composition over inheritance when it keeps the design simpler.

### SOLID Principles

- Single Responsibility Principle: each class should have one clear reason to change.
- Open/Closed Principle: code should be open for extension but closed for unnecessary modification.
- Liskov Substitution Principle: derived classes should be usable wherever their base classes are expected.
- Interface Segregation Principle: prefer small, focused interfaces over large, general-purpose ones.
- Dependency Inversion Principle: depend on abstractions rather than concrete implementations when appropriate.

### Maintainability

- Avoid duplicated logic.
- Avoid unnecessary complexity.
- Keep methods short and readable.
- Refactor when code becomes difficult to understand or extend.
- Add comments only when they explain intent, design decisions, or non-obvious logic.

### Extensibility

- Structure gameplay systems so new mechanics, characters, items, levels, or UI features can be added easily.
- Avoid hard-coding values that are likely to change.
- Prefer configurable data where appropriate.
- Separate game logic from presentation, input, and platform-specific behavior when possible.

### Code Changes

- Make minimal, focused changes for each task.
- Do not rewrite unrelated code unless necessary.
- Preserve existing behavior unless the task explicitly requires changing it.
- Do not modify user-tuned values in `Assets/Resources/GameTuningConfig.asset` unless the user explicitly asks for those value changes. When new tuning fields are required, add only the new fields and preserve all existing values.
- Keep the project organized and consistent with the existing style.
