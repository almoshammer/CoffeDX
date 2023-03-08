namespace CoffeDX.Shared
{
    public interface IQuery
    {
        IThen Insert(Exec.DGExec result);
        IThen Update(Exec.DGExec result);
        IThen Delete(Exec.DGExec result);
        IThen SelectList<T>(Exec.DGExec result);
        IThen SelectTable(Exec.DGExec result);
    }
    public interface IThen
    {
        ICatch Catch(Exec.DGException exception);
        void Finally(Exec.DGFinally _);
    }
    public interface ICatch
    {
        void Finally(Exec.DGFinally _);
    }
}
