public interface IClientApplicablePacket {
    public bool ShouldCache { get; }
    public void ApplyOnClient(int tick);
}