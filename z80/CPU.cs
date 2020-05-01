using System;

namespace JustinCredible.ZilogZ80
{
    /**
     * An emulated version of the Zilog Z80 CPU.
     */
    public partial class CPU
    {
        /**
         * Indicates the ROM has finished executing via a HLT opcode.
         * Step should not be called again without first calling Reset.
         */
        public bool Finished { get; private set; }

        /** The addressable memory; can include RAM and ROM. See CPUConfig. */
        public byte[] Memory { get; set; }

        /** The primary CPU registers (A B C D E H L) */
        public CPURegisters Registers { get; set; }

        /** Alternative register set (A' B' C' D' E' H' L') */
        public CPURegisters AlternateRegisters { get; set; } // TODO

        /** The encapsulated condition/flags register (F) */
        public ConditionFlags Flags { get; set; }

        /** Alternative flag register (F') */
        public ConditionFlags AlternateFlags { get; set; } // TODO

        /** The interrupt vector register (I) */
        public UInt16 InterruptVector { get; set; } // TODO

        /** The memory refresh register (R) */
        public UInt16 MemoryRefresh { get; set; } // TODO

        /** The index register (IX) */
        public UInt16 IndexIX { get; set; } // TODO

        /** The index register (IY) */
        public UInt16 IndexIY { get; set; } // TODO

        /** Program Counter; 16-bits */
        public UInt16 ProgramCounter { get; set; }

        /** Stack Pointer; 16-bits */
        public UInt16 StackPointer { get; set; }

        // TODO: Interrupt modes
        /** Indicates if interrupts are enabled or not. */
        public bool InterruptsEnabled { get; set; }

        /** Configuration for the CPU; used to customize the CPU instance. */
        public CPUConfig Config { get; private set; }

        // public delegate void CPUDiagDebugEvent(int eventID);

        /** Fired on CALL 0x05 when EnableCPUDiagMode is true. */
        // public event CPUDiagDebugEvent OnCPUDiagDebugEvent;

        // TODO: Is device I/O handled differently on Z80?

        /**
         * Event handler for handling CPU writes to devices.
         * 
         * Indicates the ID of the device to write the given data to.
         */
        public delegate void DeviceWriteEvent(int deviceID, byte data);

        /** Fired when the OUT instruction is encountered. */
        public event DeviceWriteEvent OnDeviceWrite;

        /**
         * Event handler for handling CPU reads from devices.
         * 
         * Indicates the ID of the device to read from and should return
         * the data for that device.
         */
        public delegate byte DeviceReadEvent(int deviceID);

        /** Fired when the IN instruction is encountered. */
        public event DeviceReadEvent OnDeviceRead;

        #region Initialization

        public CPU(CPUConfig config)
        {
            Config = config;
            this.Reset();
        }

        public void Reset()
        {
            // Re-initialize the CPU based on configuration.
            Memory = new byte[Config.MemorySize];
            Registers = Config.Registers ?? new CPURegisters();
            Flags = Config.Flags ?? new ConditionFlags();
            ProgramCounter = Config.ProgramCounter;
            StackPointer = Config.StackPointer;
            InterruptsEnabled = Config.InterruptsEnabled;

            // Reset the flag that indicates that the ROM has finished executing.
            Finished = false;
        }

        public void LoadMemory(byte[] memory)
        {
            // Ensure the memory data is not larger than we can load.
            if (memory.Length > Config.MemorySize)
                throw new Exception($"Memory cannot exceed {Config.MemorySize} bytes.");

            if (memory.Length != Config.MemorySize)
            {
                // If the memory given is less than the configured memory size, then
                // ensure that the rest of the memory array is zeroed out.
                Memory = new byte[Config.MemorySize];
                Array.Copy(memory, Memory, memory.Length);
            }
            else
                Memory = memory;
        }

        #endregion

        #region Debugging

        public void PrintDebugSummary()
        {
            var opcodeByte = ReadMemory(ProgramCounter);
            var opcodeInstruction = Opcodes.Lookup[opcodeByte].Instruction;

            var opcode = String.Format("0x{0:X2} {1}", opcodeByte, opcodeInstruction);
            var pc = String.Format("0x{0:X4}", ProgramCounter);
            var sp = String.Format("0x{0:X4}", StackPointer);
            var regA = String.Format("0x{0:X2}", Registers.A);
            var regB = String.Format("0x{0:X2}", Registers.B);
            var regC = String.Format("0x{0:X2}", Registers.C);
            var regD = String.Format("0x{0:X2}", Registers.D);
            var regE = String.Format("0x{0:X2}", Registers.E);
            var regH = String.Format("0x{0:X2}", Registers.H);
            var regL = String.Format("0x{0:X2}", Registers.L);

            var valueAtDE = Registers.DE >= Memory.Length ? "N/A" : String.Format("0x{0:X2}", ReadMemory(Registers.DE));
            var valueAtHL = Registers.HL >= Memory.Length ? "N/A" : String.Format("0x{0:X2}", ReadMemory(Registers.HL));

            Console.WriteLine($"Opcode: {opcode}");
            Console.WriteLine();
            Console.WriteLine($"PC: {pc}\tSP: {sp}");
            Console.WriteLine();
            Console.WriteLine($"A: {regA}\t\tB: {regB}\t\tC: {regC}");
            Console.WriteLine($"D: {regD}\t\tE: {regE}\t\tH: {regH}\t\tL: {regL}");
            Console.WriteLine($"(DE): {valueAtHL}\t\t\t(HL): {valueAtHL}");
            Console.WriteLine();
            Console.WriteLine($"Zero: {Flags.Zero}\tSign: {Flags.Sign}\tParity: {Flags.Parity}\tCarry: {Flags.Carry}\tAux Carry: {Flags.AuxCarry}");
        }

        #endregion

        #region Step / Execute Opcode

        /** Executes the given interrupt RST instruction and returns the number of cycles it took to execute. */
        public int StepInterrupt(Interrupt id)
        {
            switch (id)
            {
                case Interrupt.Zero:
                    ExecuteCALL(0x000, ProgramCounter);
                    return Opcodes.RST_0.Cycles;
                case Interrupt.One:
                    ExecuteCALL(0x0008, ProgramCounter);
                    return Opcodes.RST_1.Cycles;
                case Interrupt.Two:
                    ExecuteCALL(0x0010, ProgramCounter);
                    return Opcodes.RST_2.Cycles;
                case Interrupt.Three:
                    ExecuteCALL(0x0018, ProgramCounter);
                    return Opcodes.RST_3.Cycles;
                case Interrupt.Four:
                    ExecuteCALL(0x0020, ProgramCounter);
                    return Opcodes.RST_4.Cycles;
                case Interrupt.Five:
                    ExecuteCALL(0x0028, ProgramCounter);
                    return Opcodes.RST_5.Cycles;
                case Interrupt.Six:
                    ExecuteCALL(0x0030, ProgramCounter);
                    return Opcodes.RST_6.Cycles;
                case Interrupt.Seven:
                    ExecuteCALL(0x0038, ProgramCounter);
                    return Opcodes.RST_7.Cycles;
                default:
                    throw new Exception($"Unhandled interrupt ID: {id}");
            }
        }

        /** Executes the next instruction and returns the number of cycles it took to execute. */
        public int Step()
        {
            // Sanity check.
            if (Finished)
                throw new Exception("Program has finished execution; Reset() must be invoked before invoking Step() again.");

            // Fetch the next opcode to be executed, as indicated by the program counter.
            var opcode = Opcodes.GetOpcode(ProgramCounter, Memory);

            // Sanity check: unimplemented opcode?
            if (opcode == null)
                throw new Exception(String.Format("Unable to fetch opcode structure for byte 0x{0:X2} at memory address 0x{1:X4}.", Memory[ProgramCounter], ProgramCounter));

            // Indicates if we should increment the program counter after executing the instruction.
            // This is almost always the case, but there are a few cases where we don't want to.
            var incrementProgramCounter = true;

            // Some instructions have an alternate cycle count depending on the outcome of the
            // operation. This indicates if we should count the regular or alternate cycle count
            // when returning the number of cycles that the instruction took to execute.
            var useAlternateCycleCount = false;

            ExecuteOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);

            // Determine how many cycles the instruction took.

            var elapsedCycles = (UInt16)opcode.Cycles;

            if (useAlternateCycleCount)
            {
                // Sanity check; if this fails an opcode definition or implementation is invalid.
                if (opcode.AlternateCycles == null)
                    throw new Exception(String.Format("The implementation for opcode 0x{0:X2} at memory address 0x{1:X4} indicated the alternate number of cycles should be used, but was not defined.", opcode, ProgramCounter));

                elapsedCycles = (UInt16)opcode.AlternateCycles;
            }

            // Increment the program counter.
            if (incrementProgramCounter)
               ProgramCounter += (UInt16)opcode.Size;

            return elapsedCycles;
        }

        /**
         * Encapsulates the logic for executing opcodes.
         * Out parameters indicate if the program counter should be executed and which cycle count to use.
         */
        private void ExecuteOpcode(Opcode opcode, out bool incrementProgramCounter, out bool useAlternateCycleCount)
        {
            incrementProgramCounter = true;
            useAlternateCycleCount = false;

            switch (opcode.InstructionSet)
            {
                case InstructionSet.Standard:
                    ExecuteStandardOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.ExtendedStandard:
                    ExecuteExtendedStandardOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.ExtendedBit:
                    ExecuteExtendedBitOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.IX:
                    ExecuteExtendedIXOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.IY:
                    ExecuteExtendedIYOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.IXBit:
                    ExecuteExtendedIXBitOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                case InstructionSet.IYBit:
                    ExecuteExtendedIYBitOpcode(opcode, out incrementProgramCounter, out useAlternateCycleCount);
                    break;
                default:
                    throw new Exception(String.Format("Encountered an unhandled InstructionSet type {0} at memory address 0x{1:X4} when attempting to grab an opcode.", opcode.InstructionSet));
            }
        }

        #endregion

        #region Utilities

        private void SetFlags(bool carry, byte result, bool auxCarry = false)
        {
            Flags.Carry = carry;
            Flags.Zero = result == 0;
            Flags.Sign = (result & 0b10000000) == 0b10000000;
            Flags.Parity = CalculateParityBit((byte)result);
            Flags.AuxCarry = auxCarry;
        }

        private bool CalculateParityBit(byte value)
        {
            var setBits = 0;

            for (var i = 0; i < 8; i++)
            {
                if ((value & 0x01) == 1)
                    setBits++;

                value = (byte)(value >> 1);
            }

            // Parity bit is set if number of bits is even.
            return setBits == 0 || setBits % 2 == 0;
        }

        private byte ReadMemory(int address)
        {
            var mirroringEnabled = Config.MirrorMemoryStart != 0 && Config.MirrorMemoryEnd != 0;
            var error = false;

            byte? result = null;

            if (address < 0)
            {
                error = true;
            }
            else if (address < Config.MemorySize)
            {
                result = Memory[address];
            }
            else if (mirroringEnabled && address >= Config.MirrorMemoryStart && address <= Config.MirrorMemoryEnd)
            {
                var translated = address - (Config.MirrorMemoryEnd - Config.MirrorMemoryStart + 1);

                if (translated < 0 || translated >= Config.MemorySize)
                {
                    error = true;
                }
                else
                {
                    result = Memory[translated];
                }
            }
            else
            {
                error = true;
            }

            if (error)
            {
                var programCounterFormatted = String.Format("0x{0:X4}", ProgramCounter);
                var addressFormatted = String.Format("0x{0:X4}", address);
                var startAddressFormatted = String.Format("0x{0:X4}", Config.WriteableMemoryStart);
                var endAddressFormatted = String.Format("0x{0:X4}", Config.WriteableMemoryEnd);
                var mirrorEndAddressFormatted = String.Format("0x{0:X4}", Config.MirrorMemoryEnd);
                throw new Exception($"Illegal memory address ({addressFormatted}) specified for read memory operation at address {programCounterFormatted}; expected address to be between {startAddressFormatted} and {(mirroringEnabled ? mirrorEndAddressFormatted : endAddressFormatted)} inclusive.");
            }

            if (result == null)
                throw new Exception("Failed sanity check; result should be set.");

            return result.Value;
        }

        private void WriteMemory(int address, byte value)
        {
            // Determine if we should allow the write to memory based on the address
            // if the configuration has specified a restricted writeable range.
            var enforceWriteBoundsCheck = Config.WriteableMemoryStart != 0 && Config.WriteableMemoryEnd != 0;
            var mirroringEnabled = Config.MirrorMemoryStart != 0 && Config.MirrorMemoryEnd != 0;
            var allowWrite = true;
            var error = false;

            if (enforceWriteBoundsCheck)
                allowWrite = address >= Config.WriteableMemoryStart && address <= Config.WriteableMemoryEnd;

            if (allowWrite)
            {
                Memory[address] = value;
            }
            else if (mirroringEnabled && address >= Config.MirrorMemoryStart && address <= Config.MirrorMemoryEnd)
            {
                var translated = address - (Config.MirrorMemoryEnd - Config.MirrorMemoryStart + 1);

                if (translated < 0 || translated >= Config.MemorySize)
                {
                    error = true;
                }
                else
                {
                    Memory[translated] = value;
                }
            }
            else
            {
                error = true;
            }

            if (error)
            {
                var programCounterFormatted = String.Format("0x{0:X4}", ProgramCounter);
                var addressFormatted = String.Format("0x{0:X4}", address);
                var startAddressFormatted = String.Format("0x{0:X4}", Config.WriteableMemoryStart);
                var endAddressFormatted = String.Format("0x{0:X4}", Config.WriteableMemoryEnd);
                var mirrorEndAddressFormatted = String.Format("0x{0:X4}", Config.MirrorMemoryEnd);
                throw new Exception($"Illegal memory address ({addressFormatted}) specified for write memory operation at address {programCounterFormatted}; expected address to be between {startAddressFormatted} and {(mirroringEnabled ? mirrorEndAddressFormatted : endAddressFormatted)} inclusive.");
            }
        }

        #endregion
    }
}
