using BuildDataAccess.Entities;

public class UserBuild : Build
{
    public UserBuild() { }
    public UserBuild(Build build)
    {
        this.BuildId = build.BuildId;
        this.VideoId = build.VideoId;
        this.DateUpdated = build.DateUpdated;
        this.Resolution = build.Resolution;
        this.License = build.License;
        this.BuildStatus = build.BuildStatus;
        this.HasAudio = build.HasAudio;
        this.PaymentIntentId = build.PaymentIntentId;
    }
    public Guid UserObjectId { get; set; }
}