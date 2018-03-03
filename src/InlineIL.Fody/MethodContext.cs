﻿using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace InlineIL.Fody
{
    internal class MethodContext
    {
        private readonly MethodDefinition _method;

        private Collection<Instruction> Instructions => _method.Body.Instructions;

        public MethodContext(MethodDefinition method)
        {
            _method = method;
        }

        public void Process()
        {
            for (var i = 0; i < Instructions.Count; ++i)
            {
                var instruction = Instructions[i];

                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference calledMethod)
                {
                    switch (calledMethod.FullName)
                    {
                        case var name when name == MemberNames.OpNoArgMethod:
                            ProcessOpNoArg(i);
                            break;

                        case var _ when calledMethod.IsGenericInstance && calledMethod.GetElementMethod().FullName == MemberNames.PushMethod:
                            ProcessPush(i);
                            break;

                        case var name when name == MemberNames.UnreachableMethod:
                            ProcessUnreachable(i);
                            break;
                    }
                }
            }
        }

        private void ProcessOpNoArg(int instructionIndex)
        {
            var instruction = Instructions[instructionIndex];

            var opCode = OpCodeMap.FromLdsfld(instruction.Previous);

            var firstIndex = instructionIndex - 1;
            Instructions.RemoveAt(firstIndex);
            Instructions.RemoveAt(firstIndex);
            Instructions.Insert(firstIndex, Instruction.Create(opCode));
        }

        private void ProcessPush(int instructionIndex)
        {
            Instructions.RemoveAt(instructionIndex);
        }

        private void ProcessUnreachable(int instructionIndex)
        {
            Instructions.RemoveAt(instructionIndex);
            var throwInstruction = Instructions[instructionIndex];

            if (throwInstruction.OpCode != OpCodes.Throw)
                throw new InvalidOperationException("The Unreachable method should only be used like this: throw IL.Unreachable();");

            Instructions.RemoveAt(instructionIndex);
            RemoveNops(instructionIndex);
        }

        private void RemoveNops(int instructionIndex)
        {
            while (instructionIndex < Instructions.Count && Instructions[instructionIndex].OpCode == OpCodes.Nop)
                Instructions.RemoveAt(instructionIndex);

            while (--instructionIndex > 0 && Instructions[instructionIndex].OpCode == OpCodes.Nop)
                Instructions.RemoveAt(instructionIndex);
        }
    }
}
