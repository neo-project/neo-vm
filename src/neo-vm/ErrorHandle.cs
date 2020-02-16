using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace Neo.VM
{
    public class FaultState
    {
        public bool HoldError;
        public string ErrorInfo;
    }
    public sealed class ErrorHandle
    {
        public TryContext CurContext
        {
            get
            {
                return tryContext.Peek();
            }
        }
        public void Push(TryContext content)
        {
            tryContext.Push(content);
        }
        public TryContext Pop()
        {
            if (tryContext.Count == 0)
                return null;

            return tryContext.Pop();
        }
        Stack<TryContext> tryContext = new Stack<TryContext>();
        public bool HandleError(Neo.VM.ExecutionEngine engine)
        {
            for (var i = 0; i < tryContext.Count; i++)
            {
                var context = tryContext.ElementAt(i);
                if (context.State == TryState.Try)//当前捕获程序在try段
                {
                    if (context.HasCatch)
                    {
                        context.State = TryState.Catch;
                        //首先需要恢复栈状态到context
                        ResumeContext(engine, context);
                        engine.CurrentContext.InstructionPointer = context.CatchPointer;
                        return true;
                    }
                    else
                    {
                        //挂起錯誤
                        context.State = TryState.Catch;
                        engine.ExecuteEndTryCatch(TryState.Catch);
                        engine.FaultState.HoldError = true;
                        engine.State = VMState.NONE;
                        continue;
                    }
                }
                else if (context.State == TryState.Catch)//当前捕获程序在catch段，保留这个异常，并调用finally
                {
                    //首先需要恢复栈状态到context
                    ResumeContext(engine, context);
                    engine.ExecuteEndTryCatch(TryState.Catch);
                    engine.FaultState.HoldError = true;
                    engine.State = VMState.NONE;
                    continue;
                }
                else if (context.State == TryState.Finally)//不处理
                {
                    continue;
                }
            }
            return false;
        }
        void ResumeContext(Neo.VM.ExecutionEngine engine, TryContext context)
        {
            while (engine.CurrentContext != context.ExecutionContext)
            {
                var executionContext = engine.InvocationStack.Pop();
                engine.ContextUnloaded(executionContext);
            }
        }
    }
}
