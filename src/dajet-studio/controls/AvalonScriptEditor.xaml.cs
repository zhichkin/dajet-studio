using DaJet.Data.Scripting;
using DaJet.Data.Scripting.Wpf;
using ICSharpCode.AvalonEdit.Folding;
using System.Windows;
using System.Windows.Controls;

namespace DaJet.Studio
{
    public partial class AvalonScriptEditor : UserControl, IParserErrorHandler
    {
        //private FoldingManager foldingManager;
        private readonly ScriptingClient ScriptingClient;
        //private readonly SelectStatementFoldingStrategy foldingStrategy = new SelectStatementFoldingStrategy();

        public AvalonScriptEditor(string script, IScriptingService scripting)
        {
            InitializeComponent();

            textEditor.Document.Text = script;

            ScriptingClient = new ScriptingClient(scripting, this);

            textEditor.TextArea.KeyDown += ScriptingClient.TextArea_KeyDown;
            textEditor.TextArea.TextEntered += ScriptingClient.TextArea_TextEnteredHandler;
            textEditor.TextArea.TextEntering += ScriptingClient.TextArea_TextEnteringHandler;

            //DataObject.AddPastingHandler(textEditor.TextArea, ScriptingClient.TextArea_TextPasteHandler);
            //textEditor.TextArea.TextView.MouseHover += ScriptingClient.TextView_MouseHoverHandler;
            //textEditor.TextArea.TextView.MouseHoverStopped += ScriptingClient.TextView_MouseHoverStoppedHandler;

            //textEditor.Load(dialog.FileName);
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor.SyntaxHighlighting = ScriptingClient.GetSyntaxHighlightingDefinition();

            //foldingManager = FoldingManager.Install(textEditor.TextArea);

            //foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
        }
        public void HandleError(string message)
        {
            warningsBlock.Text = message;
        }
    }
}