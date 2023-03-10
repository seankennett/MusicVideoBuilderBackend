namespace NewVideoFunction.Interfaces
{
    public interface IMailer
    {
        void Send(string username, string emailAddress, string blobSasUrl, string videoName);
    }
}
