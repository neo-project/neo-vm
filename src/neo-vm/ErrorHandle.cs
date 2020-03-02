using System.Collections.Generic;

namespace Neo.VM
{
    public sealed class ErrorHandle
    {
        Stack<TryContext> tryStack = new Stack<TryContext>();

        public TryContext CurContext
        {
            get
            {
                return tryStack.Peek();
            }
        }

        public void Push(TryContext content)
        {
            tryStack.Push(content);
        }

        public TryContext Pop()
        {
            if (tryStack.Count == 0)
                return null;

            return tryStack.Pop();
        }

        public bool HandleError(ExecutionEngine engine)
        {
            while (tryStack.TryPeek(out TryContext context))
            {
                switch (context.State)
                {
                    case TryState.Try:
                        {
                            if (context.HasCatch)
                            {
                                context.State = TryState.Catch;
                                ResumeContext(engine, context);
                                engine.CurrentContext.InstructionPointer = context.CatchPointer;
                                return true;
                            }
                            else
                            {
                                ResumeContext(engine, context);
                                engine.ExecuteEndTryCatch(TryState.Try);
                                engine.FaultState.Rethrow = true;
                                return true;
                            }
                        }
                    case TryState.Catch:
                        {
                            if (!context.HasFinally) break;

                            ResumeContext(engine, context);
                            engine.ExecuteEndTryCatch(TryState.Catch);
                            engine.FaultState.Rethrow = true;
                            return true;
                        }
                    case TryState.Finally:
                    default:
                        break;
                }
                tryStack.Pop();
            }
            return false;
        }

        void ResumeContext(ExecutionEngine engine, TryContext context)
        {
            while (engine.CurrentContext != context.ExecutionContext)
            {
                var executionContext = engine.PopExecutionContext();
                //engine.InvocationStack.Pop();
                engine.ContextUnloaded(executionContext);
            }
        }
    }
}
