namespace DaJet.Studio.MVVM
{
    public interface ITreeNodeController
    {
        void Search(string text);
        TreeNodeViewModel CreateTreeNode(MainWindowViewModel parent);
        TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent);
    }
}