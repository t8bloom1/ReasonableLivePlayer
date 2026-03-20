using System.Collections.ObjectModel;
using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Models;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 6: Playlist reorder preserves contents
/// Validates: Requirements 6.3
/// </summary>
public class PlaylistReorderTests
{
    /// <summary>
    /// For any playlist of songs and for any valid move operation (source index,
    /// destination index), after the move the playlist should contain the same songs
    /// and the moved song should be at the destination index.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Reorder_Preserves_Contents_And_Places_Song()
    {
        return Prop.ForAll(
            Arb.From(
                from count in Gen.Choose(2, 30)
                from srcIdx in Gen.Choose(0, count - 1)
                from dstIdx in Gen.Choose(0, count - 1)
                where srcIdx != dstIdx
                select (count, srcIdx, dstIdx)
            ),
            tuple =>
            {
                var (count, srcIdx, dstIdx) = tuple;
                var songs = new ObservableCollection<Song>();
                for (int i = 0; i < count; i++)
                    songs.Add(new Song { FilePath = $@"C:\Music\song{i}.reason" });

                var originalPaths = songs.Select(s => s.FilePath).OrderBy(p => p).ToList();
                var movedSong = songs[srcIdx];

                songs.Move(srcIdx, dstIdx);

                var afterPaths = songs.Select(s => s.FilePath).OrderBy(p => p).ToList();
                return originalPaths.SequenceEqual(afterPaths)
                    && songs[dstIdx] == movedSong;
            });
    }
}
