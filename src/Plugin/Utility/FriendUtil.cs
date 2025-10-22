using System;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using GoodFriend.Plugin.Base;

namespace GoodFriend.Plugin.Utility;

internal static class FriendUtil
{
    /// <summary>
    ///     Gets a friend using a hashed Content ID + Salt used when initially hashed.
    /// </summary>
    /// <param name="friends">The friend list to check hashes on</param>
    /// <param name="hash">The hashed Content ID to search for.</param>
    /// <param name="salt">The salt used to hash the original content ID.</param>
    /// <returns>The friend's CharacterData if matched.</returns>
    public static InfoProxyCommonList.CharacterData? GetFriendFromHash(ReadOnlySpan<InfoProxyCommonList.CharacterData> friends, byte[] hash, byte[] salt)
    {
        foreach (var friend in friends)
        {
            if (CryptoUtil.HashValueWithSalt(friend.ContentId, salt) == hash)
            {
                return friend;
            }
        }
        return null;
    }
}
