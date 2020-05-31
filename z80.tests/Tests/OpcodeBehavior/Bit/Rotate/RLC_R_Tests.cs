using Xunit;

namespace JustinCredible.ZilogZ80.Tests
{
    public class RLC_R_Tests : BaseTest
    {
        [Theory]
        [ClassData(typeof(RegistersClassData))]
        public void Test_RLC_R_SetsCarryFlagTrue(Register register)
        {
            var rom = AssembleSource($@"
                org 00h
                RLC {register}
                HALT
            ");

            var initialState = new CPUConfig()
            {
                Registers = new CPURegisters()
                {
                    [register] = 0b11100100,
                },
                Flags = new ConditionFlags()
                {
                    // Should be affected.
                    Carry = false,
                    Sign = false,
                    Zero = true,
                    Parity = false,

                    // Should be reset.
                    Subtract = true,
                    AuxCarry = true,
                }
            };

            var state = Execute(rom, initialState);

            Assert.Equal(0b11001001, state.Registers[register]);

            // Should be affected.
            Assert.True(state.Flags.Carry);
            Assert.True(state.Flags.Sign);
            Assert.False(state.Flags.Zero);
            Assert.True(state.Flags.Parity);

            // Should be reset.
            Assert.False(state.Flags.AuxCarry);
            Assert.False(state.Flags.Subtract);

            Assert.Equal(2, state.Iterations);
            Assert.Equal(4 + 8, state.Cycles);
            Assert.Equal(0x02, state.Registers.PC);
        }

        [Theory]
        [ClassData(typeof(RegistersClassData))]
        public void Test_RLC_R_SetsCarryFlagFalse(Register register)
        {
            var rom = AssembleSource($@"
                org 00h
                RLC {register}
                HALT
            ");

            var initialState = new CPUConfig()
            {
                Registers = new CPURegisters()
                {
                    [register] = 0b01100101,
                },
                Flags = new ConditionFlags()
                {
                    // Should be affected.
                    Carry = true,
                    Sign = false,
                    Zero = true,
                    Parity = false,

                    // Should be reset.
                    Subtract = true,
                    AuxCarry = true,
                }
            };

            var state = Execute(rom, initialState);

            Assert.Equal(0b11001010, state.Registers[register]);

            // Should be affected.
            Assert.False(state.Flags.Carry);
            Assert.True(state.Flags.Sign);
            Assert.False(state.Flags.Zero);
            Assert.True(state.Flags.Parity);

            // Should be reset.
            Assert.False(state.Flags.AuxCarry);
            Assert.False(state.Flags.Subtract);

            Assert.Equal(2, state.Iterations);
            Assert.Equal(4 + 8, state.Cycles);
            Assert.Equal(0x02, state.Registers.PC);
        }

        [Fact]
        public void Test_RLC_HL_SetsCarryFlagTrue()
        {
            var rom = AssembleSource($@"
                org 00h
                RLC (HL)
                HALT
            ");

            var memory = new byte[16*1024];
            memory[0x2234] = 0b11100100;

            var initialState = new CPUConfig()
            {
                Registers = new CPURegisters()
                {
                    HL = 0x2234,
                },
                Flags = new ConditionFlags()
                {
                    // Should be affected.
                    Carry = false,
                    Sign = false,
                    Zero = true,
                    Parity = false,

                    // Should be reset.
                    Subtract = true,
                    AuxCarry = true,
                }
            };

            var state = Execute(rom, memory, initialState);

            Assert.Equal(0b11001001, state.Memory[0x2234]);

            // Should be affected.
            Assert.True(state.Flags.Carry);
            Assert.True(state.Flags.Sign);
            Assert.False(state.Flags.Zero);
            Assert.True(state.Flags.Parity);

            // Should be reset.
            Assert.False(state.Flags.AuxCarry);
            Assert.False(state.Flags.Subtract);

            Assert.Equal(2, state.Iterations);
            Assert.Equal(4 + 15, state.Cycles);
            Assert.Equal(0x02, state.Registers.PC);
        }

        [Fact]
        public void Test_RLC_HL_SetsCarryFlagFalse()
        {
            var rom = AssembleSource($@"
                org 00h
                RLC (HL)
                HALT
            ");

            var memory = new byte[16*1024];
            memory[0x2234] = 0b01100101;

            var initialState = new CPUConfig()
            {
                Registers = new CPURegisters()
                {
                    HL = 0x2234,
                },
                Flags = new ConditionFlags()
                {
                    // Should be affected.
                    Carry = true,
                    Sign = false,
                    Zero = true,
                    Parity = false,

                    // Should be reset.
                    Subtract = true,
                    AuxCarry = true,
                }
            };

            var state = Execute(rom, memory, initialState);

            Assert.Equal(0b11001010, state.Memory[0x2234]);

            // Should be affected.
            Assert.False(state.Flags.Carry);
            Assert.True(state.Flags.Sign);
            Assert.False(state.Flags.Zero);
            Assert.True(state.Flags.Parity);

            // Should be reset.
            Assert.False(state.Flags.AuxCarry);
            Assert.False(state.Flags.Subtract);

            Assert.Equal(2, state.Iterations);
            Assert.Equal(4 + 15, state.Cycles);
            Assert.Equal(0x02, state.Registers.PC);
        }
    }
}