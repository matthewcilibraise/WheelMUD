﻿//-----------------------------------------------------------------------------
// <copyright file="PickTalentsState.cs" company="WheelMUD Development Team">
//   Copyright (c) WheelMUD Development Team.  See LICENSE.txt.  This file is 
//   subject to the Microsoft Public License.  All other rights reserved.
// </copyright>
// <summary>
//   Character creation state used to set the initial talents for the player.
// </summary>
//-----------------------------------------------------------------------------

namespace WarriorRogueMage.CharacterCreation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WheelMUD.ConnectionStates;
    using WheelMUD.Core;

    /// <summary>The character creation step where the player will pick their talents.</summary>
    public class PickTalentsState : CharacterCreationSubState
    {
        private int longestTalentName;
        private readonly string formattedTalents;
        private readonly List<Talent> talents;

        /// <summary>Initializes a new instance of the <see cref="PickTalentsState"/> class.</summary>
        /// <param name="session">The session.</param>
        public PickTalentsState(Session session)
            : base(session)
        {
            this.Session.Write("You will now pick your character's starting talent.");
            this.talents = TalentFinder.Instance.NormalTalents;
            this.formattedTalents = this.FormatTalentText();
            this.RefreshScreen(false);
        }

        /// <summary>ProcessInput is used to receive the user input during this state.</summary>
        /// <param name="command">The command text to be processed.</param>
        public override void ProcessInput(string command)
        {
            var commandParts = command.Split(' ');
            var currentCommand = commandParts[0];
            switch (currentCommand)
            {
                case "view":
                    if (commandParts.Length > 1)
                    {
                        this.ViewTalentDescription(commandParts[1]);
                    }
                    else
                    {
                        WrmChargenCommon.SendErrorMessage(this.Session, "Please select which talent you would like to view details for, like 'view sailor'.");
                    }
                    break;
                case "list":
                    this.RefreshScreen();
                    break;
                default:
                    var talent = this.GetTalent(currentCommand);
                    if (talent != null)
                    {
                        // @@@ TODO: Save talent to a WRM-specific Behavior?
                        this.StateMachine.HandleNextStep(this, StepStatus.Success);
                    }
                    else
                    {
                        WrmChargenCommon.SendErrorMessage(this.Session, "Invalid talent. Try again, or use 'view [talent]' or 'list'.");
                    }
                    break;
            }
        }

        public override string BuildPrompt()
        {
            return "Select the character's starting talent.\n> ";
        }

        private Talent GetTalent(string talentName)
        {
            return (from r in this.talents
                    where r.Name.Equals(talentName, StringComparison.OrdinalIgnoreCase)
                    select r).FirstOrDefault();
        }

        private void ViewTalentDescription(string talent)
        {
            string talentToView = talent.Replace("view ", string.Empty);

            Talent foundTalent = this.talents.Find(s => s.Name.Equals(talentToView, StringComparison.CurrentCultureIgnoreCase));
            if (foundTalent != null)
            {
                var sb = new StringBuilder();
                sb.Append("<%b%><%yellow%>=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=" + Environment.NewLine);
                sb.AppendFormat("Description for {0}" + Environment.NewLine, foundTalent.Name);
                sb.Append("=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=<%n%>" + Environment.NewLine);
                sb.Append("<%b%><%white%>" + foundTalent.Description + Environment.NewLine);
                sb.Append("<%b%><%yellow%>=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=<%n%>");

                this.Session.Write(sb.ToString());
            }
            else
            {
                WrmChargenCommon.SendErrorMessage(this.Session, "That talent does not exist.");
            }
        }

        private string FormatTalentText()
        {
            var talentQueue = new Queue<Talent>();
            var text = new StringBuilder();

            foreach (var gameTalent in this.talents)
            {
                // Find out what talent name is the longest
                if (gameTalent.Name.Length > this.longestTalentName)
                {
                    this.longestTalentName = gameTalent.Name.Length;
                }

                talentQueue.Enqueue(gameTalent);
            }

            int rows = this.talents.Count / 4;

            try
            {
                for (int i = 0; i < rows; i++)
                {
                    string talent1 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                    string talent2 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                    string talent3 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                    string talent4 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                    text.AppendFormat("{0}  {1}  {2}  {3}" + Environment.NewLine, talent1, talent2, talent3, talent4);
                }

                if ((rows % 4) > 0)
                {
                    int columns = rows - (rows * 4);

                    switch (columns)
                    {
                        case 1:
                            string talentcolumn1 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            text.AppendFormat("{0}" + Environment.NewLine, talentcolumn1);
                            break;
                        case 2:
                            string tk1 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            string tk2 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            text.AppendFormat("{0}  {1}" + Environment.NewLine, tk1, tk2);
                            break;
                        case 3:
                            string tkl1 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            string tkl2 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            string tkl3 = WrmChargenCommon.FormatToColumn(this.longestTalentName, talentQueue.Dequeue().Name);
                            text.AppendFormat("{0}  {1}  {2}" + Environment.NewLine, tkl1, tkl2, tkl3);
                            break;
                    }
                }
            }
            catch
            {
            }

            text.Append(Environment.NewLine);
            return text.ToString();
        }

        private void RefreshScreen(bool sendPrompt = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("You may pick one starting talent for your character.");
            sb.AppendLine("<%green%>Please select 1 from the list below:<%n%>");
            sb.AppendLine(this.formattedTalents);
            sb.AppendLine("<%yellow%>==========================================================================");
            sb.AppendLine("To pick a talent, type the talent's name. Example: sailor");
            sb.AppendLine("To view a talent's description use the view command. Example: view sailor");
            sb.AppendLine("To see this screen again type list.");
            sb.AppendLine("==========================================================================<%n%>");
            this.Session.Write(sb.ToString(), sendPrompt);
        }
    }
}