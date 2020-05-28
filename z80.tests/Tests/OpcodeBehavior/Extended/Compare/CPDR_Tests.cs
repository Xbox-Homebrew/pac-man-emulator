using System.Collections.Generic;
using Xunit;

namespace JustinCredible.ZilogZ80.Tests
{
    public class CPDR_Tests : BaseTest
    {
        [Fact]
        public void Test_CPDR()
        {
            var rom = AssembleSource($@"
                org 00h
                CPDR
                HALT
            ");

            var memory = new byte[16*1024];
            memory[0x1118] = 0x52;
            memory[0x1117] = 0x00;
            memory[0x1116] = 0xF3;

            var initialState = new CPUConfig()
            {
                Registers = new CPURegisters()
                {
                    A = 0xF3,
                    BC = 0x0007,
                    HL = 0x1118,
                },
                Flags = new ConditionFlags()
                {
                    // Should remain unaffected.
                    Carry = true,

                    // Should be affected.
                    Zero = false,
                    Subtract = false,
                    Sign = true,
                    // AuxCarry = true, // TODO
                    Parity = true,
                },
                MemorySize = memory.Length,
            };

            var cpu = new CPU(initialState);

            var state = Execute(rom, memory, cpu);

            Assert.Equal(0x0004, state.Registers.BC);
            Assert.Equal(0x1115, state.Registers.HL);
            Assert.Equal(0x52, state.Memory[0x1118]);
            Assert.Equal(0x00, state.Memory[0x1117]);
            Assert.Equal(0xF3, state.Memory[0x1116]);

            // Should remain unaffected.
            Assert.True(state.Flags.Carry);

            // Should be affected.
            Assert.True(state.Flags.Zero);
            Assert.True(state.Flags.Subtract);
            Assert.False(state.Flags.Sign);
            // Assert.True(state.Flags.AuxCarry); // TODO
            Assert.True(state.Flags.Parity);

            Assert.Equal(4, state.Iterations);
            Assert.Equal(4 + (21 * 2) + 16, state.Cycles);
            Assert.Equal(0x02, state.Registers.PC);
        }
    }
}