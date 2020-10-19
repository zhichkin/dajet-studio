namespace DaJet.Studio.MVVM
{
    public interface ITreeNodeController
    {
        TreeNodeViewModel CreateTreeNode();
        TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent);
    }
}