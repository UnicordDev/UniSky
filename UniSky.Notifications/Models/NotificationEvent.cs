using FishyFlip.Models;

namespace UniSky.Notifications.Models;

public record class NotificationEvent(
    string Operation,
    string SourceType,
    string SourceCollection,
    ATDid SourceDid,
    ATUri? SourceRecordUri,
    ATDid SubjectDid,
    ATUri SubjectRecordUri
    );