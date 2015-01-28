using MBC.App.Terminal.Controls;
using MBC.App.Terminal.Layouts;
using MBC.Core;
using MBC.Core.Controllers;
using MBC.Core.Game;
using MBC.Core.Util;
using MBC.Shared;
using MBC.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBC.App.Terminal.Modules
{
    public class CompetitionOptionsDisplay : TerminalModule
    {
        private ControllerSkeleton blue;
        private VerticalLayout layout;
        private NumericControl millisecondControl;
        private ControllerSkeleton red;
        private NumericControl roundsNumberControl;
        private CheckboxControl showBoards;

        // New match values
        private const int defaultRounds = 1000;//number of rounds
        private const int roundsIncrement = 10;
        private const int defaultInterval = 0; // interval between rounds, in milliseconds 
        private const int intervalIncrement = 25; // interval between rounds, in milliseconds 

        public CompetitionOptionsDisplay(ControllerSkeleton red, ControllerSkeleton blue)
        {
            this.red = red;
            this.blue = blue;
            layout = new VerticalLayout(VerticalLayout.VerticalAlign.Center);
            roundsNumberControl = new NumericControl(new NumericControl.NumericControlParameters("# of rounds", false, 1, 100000, roundsIncrement, defaultRounds));
            millisecondControl = new NumericControl(new NumericControl.NumericControlParameters("Millisecond delay", false, 0, 10000, intervalIncrement, defaultInterval));
            showBoards = new CheckboxControl("Show ASCII boards");
           
            layout.Add(roundsNumberControl);
            //layout.Add(new CheckboxControl("Play out rounds"));
            layout.Add(millisecondControl);
            layout.Add(showBoards);
            layout.Add(new ButtonControl("Confirm", ButtonConfirmEvent));
            AddControlLayout(layout);
        }

        protected override void Display()
        {
            var redName = red.GetAttribute<NameAttribute>().Name;
            var blueName = blue.GetAttribute<NameAttribute>().Name;
            Console.ForegroundColor = ConsoleColor.White;
            WriteCenteredText("=====COMPETITION SETTINGS=====");
            NewLine();

            Console.ForegroundColor = ConsoleColor.Red;
            WriteText(redName);

            Console.ForegroundColor = ConsoleColor.Green;
            WriteCenteredText("VS.");

            Console.ForegroundColor = ConsoleColor.Cyan;
            AlignToCoord(Width - blueName.Length - 1, CurrentY);
            WriteText(blueName);
            NewLine();

            Console.ForegroundColor = ConsoleColor.White;
            WriteCenteredText("Review the available settings for this matchup.");
            NewLine(2);
            layout.Display();
        }

        private bool ButtonConfirmEvent(string btn)
        {
            Configuration.Global.SetValue("mbc_match_rounds", roundsNumberControl.Value.ToString());
            var match = new ClassicMatch();
            match.PlayerCreate(red);
            match.PlayerCreate(blue);
            CompetitionRun runner = new CompetitionRun(match, millisecondControl.Value, showBoards.IsChecked);
            BattleshipConsole.AddModule(runner);
            BattleshipConsole.RemoveModule(this);
            BattleshipConsole.UpdateDisplay();
            runner.Begin();
            return true;
        }
    }
}