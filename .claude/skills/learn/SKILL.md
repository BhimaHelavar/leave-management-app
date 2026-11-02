---
name: learn
description: "Create the study notes and interview Q&A for a finished spec, and show a short version in chat. Use after a spec is implemented and reviewed, or when the owner asks to learn or revise a feature."
argument-hint: "[spec folder name, for example 4c-dev-authentication]"
disable-model-invocation: true
---

Create the learning material for `$ARGUMENTS`.

1. If the argument is empty, use the spec marked `Review` or the most recently finished one in `ai/specs/INDEX.md`, and say which one you chose.
2. Delegate to the `learning-coach` subagent to write `ai/learning/$ARGUMENTS.md`.
3. In the chat, give a short version for the owner, in this order: what it is, how it works, why it is like that, when to use it. Add one small diagram and one example from the app. Then list the 3 teach-back questions and the exercise.
4. Tell the owner where the full note is. Ask them to answer the teach-back questions in their own words. Do not answer them.
