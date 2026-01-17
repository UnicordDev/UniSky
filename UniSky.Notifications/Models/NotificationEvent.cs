using FishyFlip.Models;
using UniSky.Notifications.Data;

namespace UniSky.Notifications.Models;

public record class NotificationEvent(
    string Operation,
    string SourceType,
    string SourceCollection,
    ATDid SourceDid,
    ATUri? SourceRecordUri,
    ATDid SubjectDid,
    ATUri SubjectRecordUri,
    NotificationRegistration Registration
    );