using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FishyFlip.Models;

namespace UniSky.Models;

public record class SessionModel
{
    public bool IsActive { get; init; }
    public string Service { get; init; }
    public string DID { get; init; }
    public string RefreshJwt { get; init; }
    public string AccessJwt { get; init; }
    public string Handle { get; init; }
    public string EmailAddress { get; init; }
    public string ProofKey { get; init; }

    [JsonConstructor]
    public SessionModel(bool isActive, string service, string did, string refreshJwt, string accessJwt, string handle, string emailAddress, string? proofKey)
    {
        IsActive = isActive;
        Service = service;
        DID = did;
        RefreshJwt = refreshJwt;
        AccessJwt = accessJwt;
        Handle = handle;
        EmailAddress = emailAddress;
        ProofKey = proofKey;
    }

    public SessionModel(bool isActive, string service, Session session, AuthSession? authSession = null)
        : this(isActive, service, session.Did.Handler, session.RefreshJwt, session.AccessJwt, session.Handle.Handle, session.Email, authSession?.ProofKey) { }

    [JsonIgnore]
    public AuthSession Session
        => new AuthSession(new Session(new ATDid(DID), null, new ATHandle(Handle), EmailAddress, AccessJwt, RefreshJwt), this.ProofKey);
}
