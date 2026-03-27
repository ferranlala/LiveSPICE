using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements a behavioral model of a Bucket Brigade Device (BBD) analog delay line.
    /// Each stage delays the signal by one simulation timestep.
    /// </summary>
    [Category("ICs")]
    [DisplayName("Bucket Brigade Device")]
    [Description("Analog delay line using bucket-brigade technology. Each stage delays the signal by one simulation timestep.")]
    public class BucketBrigadeDevice : Component
    {
        private Terminal inp, outp;

        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return inp;
                yield return outp;
            }
        }

        [Browsable(false)]
        public Terminal In { get { return inp; } }
        [Browsable(false)]
        public Terminal Out { get { return outp; } }

        public BucketBrigadeDevice()
        {
            inp = new Terminal(this, "In");
            outp = new Terminal(this, "Out");
        }

        private int stages = 256;
        [Serialize, Description("Number of delay stages.")]
        public int Stages
        {
            get { return stages; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "Stages must be at least 1.");
                stages = value;
                NotifyChanged(nameof(Stages));
            }
        }

        public override void Analyze(Analysis Mna)
        {
            // Unknown output current.
            Mna.AddTerminal(Out, Mna.AddUnknown("i" + Name));

            if (Stages == 1)
            {
                // Single stage: simple delay like DelayBuffer.
                Mna.AddEquation(In.V.Evaluate(t, t - T), Out.V);
            }
            else
            {
                // Create internal nodes for the delay chain.
                Node[] stageNodes = new Node[Stages - 1];
                for (int i = 0; i < Stages - 1; i++)
                    stageNodes[i] = new Node() { Name = "s" + i.ToString() };

                Mna.PushContext(Name, stageNodes);

                // First stage: delayed input.
                Mna.AddEquation(In.V.Evaluate(t, t - T), stageNodes[0].V);

                // Intermediate stages.
                for (int i = 1; i < Stages - 1; i++)
                    Mna.AddEquation(stageNodes[i - 1].V.Evaluate(t, t - T), stageNodes[i].V);

                // Last stage: connect delayed last internal node to output.
                Mna.AddEquation(stageNodes[Stages - 2].V.Evaluate(t, t - T), Out.V);

                Mna.PopContext();
            }
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal inp, Terminal outp, Func<string> Name, Func<string> Part)
        {
            // Input on left, Output on right.
            Sym.AddTerminal(inp, new Coord(-30, 0), new Coord(-20, 0));
            Sym.AddTerminal(outp, new Coord(30, 0), new Coord(20, 0));

            // IC rectangle body.
            Sym.AddRectangle(EdgeType.Black, new Coord(-20, -15), new Coord(20, 15));

            // Labels.
            Sym.DrawText(() => "BBD", new Coord(0, 0), Alignment.Center, Alignment.Center);
            Sym.DrawText(Name, new Coord(0, 20), Alignment.Center, Alignment.Near);
            if (Part != null)
                Sym.DrawText(Part, new Coord(0, -20), Alignment.Center, Alignment.Far);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            LayoutSymbol(Sym, In, Out, () => Name, () => PartNumber);
        }
    }
}
