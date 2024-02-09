using System;

[Serializable]
public struct GetOfferResponse
{
    public UInt16 clientId;

    public string sdp;

    public GetOfferResponse(UInt16 id, string offer)
    {
        clientId = id;
        sdp = offer;
    }
}