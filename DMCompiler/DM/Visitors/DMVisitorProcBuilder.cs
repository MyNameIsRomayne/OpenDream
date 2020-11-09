﻿using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DMCompiler.DM.Visitors {
    class DMVisitorProcBuilder : DMASTVisitor {
        private DMProc _proc;
        private Stack<object> _valueStack = new Stack<object>();

        public DMProc BuildProc(DMASTProcDefinition procDefinition) {
            _proc = new DMProc();

            if (procDefinition.Body != null) {
                _valueStack.Clear();
                procDefinition.Body.Visit(this);
            }

            return _proc;
        }

        public void VisitProcBlockInner(DMASTProcBlockInner block) {
            foreach (DMASTProcStatement statement in block.Statements) {
                statement.Visit(this);
            }
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statement) {
            statement.Expression.Visit(this);
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                statement.Value.Visit(this);
            } else {
                _proc.PushNull();
            }

            _proc.Return();
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            procCall.Callable.Visit(this);
            PushCallParameters(procCall.Parameters);
            _proc.Call();
        }

        public void VisitCallParameter(DMASTCallParameter parameter) {
            parameter.Value.Visit(this);
        }

        public void VisitCallableIdentifier(DMASTCallableIdentifier identifier) {
            _proc.GetIdentifier(identifier.Identifier);
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            _proc.PushSuperProc();
        }

        public void VisitAssign(DMASTAssign assign) {
            assign.Expression.Visit(this);
            assign.Value.Visit(this);
            _proc.Assign();
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            newDereference.Dereference.Visit(this);
            PushCallParameters(newDereference.Parameters);
            _proc.CreateObject();
        }

        public void VisitCallableDereference(DMASTCallableDereference dereference) {
            dereference.Left.Visit(this);

            foreach (DMASTCallableIdentifier identifier in dereference.Dereferences) {
                _proc.Dereference(identifier.Identifier);
            }
        }

        public void VisitAdd(DMASTAdd add) {
            add.A.Visit(this);
            add.B.Visit(this);
            _proc.Add();
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            subtract.A.Visit(this);
            subtract.B.Visit(this);
            _proc.Subtract();
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            leftShift.A.Visit(this);
            leftShift.B.Visit(this);
            _proc.BitShiftLeft();
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            _proc.PushInt(constant.Value);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            _proc.PushString(constant.Value);
        }

        public void VisitConstantNull(DMASTConstantNull constant) {
            _proc.PushNull();
        }

        private void PushCallParameters(DMASTCallParameter[] parameters) {
            List<DreamProcOpcodeParameterType> parameterTypes = new List<DreamProcOpcodeParameterType>();
            List<string> parameterNames = new List<string>();
            for (int i = parameters.Length - 1; i >= 0; i--) { //Push arguments backwards
                DMASTCallParameter parameter = parameters[i];
                parameter.Visit(this);

                if (parameter.Name != null) {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Named);
                    parameterNames.Add(parameter.Name);
                } else {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Unnamed);
                }
            }

            _proc.PushArguments(parameters.Length, parameterTypes.ToArray(), parameterNames.ToArray());
        }
    }
}
