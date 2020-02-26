using System;
using System.Collections.Generic;
using System.Linq;
namespace Neo.VM
{
    public class FaultState
    {
        public bool Rethrow;
        public Exception Error;
    }

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
            for (var i = 0; i < tryStack.Count; i++)
            {
                var context = tryStack.ElementAt(i);
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
                                context.State = TryState.Catch;
                                engine.ExecuteEndTryCatch(TryState.Catch);
                                engine.FaultState.Rethrow = true;
                                break;
                            }
                        }
                    case TryState.Catch:
                        {
                            ResumeContext(engine, context);
                            engine.ExecuteEndTryCatch(TryState.Catch);
                            engine.FaultState.Rethrow = true;
                            break;
                        }
                    case TryState.Finally:
                    default:
                        break;
                }
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
