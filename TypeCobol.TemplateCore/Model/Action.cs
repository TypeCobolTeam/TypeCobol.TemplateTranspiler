﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeCobol.TemplateCore.Model
{
    /// <summary>
    /// An action
    /// </summary>
    public class Action : AttributedEntity, ITranspilable
    {
        /// <summary>
        /// The action's Script code
        /// </summary>
        public string Code
        {
            get;
            set;
        }

        /// <summary>
        /// The Action C# Code
        /// </summary>
        public string CSharpCode
        {
            get;
            set;
        }

        private string _TranspiledCode;
        /// <summary>
        /// Code Transpilé d'une Action.
        /// </summary>
        public string TranspiledCode
        {
            get
            {
                if (_TranspiledCode == null)
                {
                    if (Attributes.ContainsKey(AttributeNames.Action))
                    {
                        string name = Attributes.ContainsKey(AttributeNames.Name) ? $@"""{(string)Attributes[AttributeNames.Name].Value}""" : "null";
                        string action = $@"""{(string)Attributes[AttributeNames.Action].Value}""";
                        string group = Attributes.ContainsKey(AttributeNames.Group) ? $@"""{(string)Attributes[AttributeNames.Group].Value}""" : "null";
                        string location = Attributes.ContainsKey(AttributeNames.Location) ? $@"""{(string)Attributes[AttributeNames.Location].Value}""" : "null";
                        string position = Attributes.ContainsKey(AttributeNames.Position) ? $@"""{(string)Attributes[AttributeNames.Position].Value}""" : "null";
                        string newline = Attributes.ContainsKey(AttributeNames.NewLine) ? ("true".Equals((string)Attributes[AttributeNames.NewLine].Value) ? "true" : "false") : "false";
                        _TranspiledCode = $@"TypeCobol.Codegen.Actions.Action @SelfAction = @SelfContext.CreateAction(@Self, {name}, @SelfResult.ToString(), {action}, {group}, {location}, {position}, {newline});";
                    }
                }
                return _TranspiledCode;
            }
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public Action()
        {
            Code = "";
        }

        /// <summary>
        /// Code constructor.
        /// </summary>
        /// <param name="code"></param>
        public Action(string code)
        {
            Code = code;
        }        
    }
}
