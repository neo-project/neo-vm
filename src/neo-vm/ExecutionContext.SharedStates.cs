using System;
using System.Collections.Generic;

namespace Neo.VM
{
    partial class ExecutionContext
    {
        private class SharedStates
        {
            public readonly Script Script;
            public readonly EvaluationStack EvaluationStack;
            public Slot? StaticFields;
            public readonly Dictionary<Type, object> States;

            public SharedStates(Script script, ReferenceCounter referenceCounter)
            {
                this.Script = script;
                this.EvaluationStack = new EvaluationStack(referenceCounter);
                this.States = new Dictionary<Type, object>();
            }
        }
    }
}
