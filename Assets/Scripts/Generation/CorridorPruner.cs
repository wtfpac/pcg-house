using System;

namespace PCG.Generation
{
    public static class CorridorPruner
    {
        public static bool Prune(FloorPlan plan, HouseSettings s)
        {
            Room hall = plan.Find(RoomType.Hall);

            if (hall == null || hall.Parts.Count != 1)
                return false;

            int hallIndex = plan.Rooms.IndexOf(hall);
            CellMap map = new CellMap(plan);
            Area corridor = hall.Parts[0];
            bool alongX = corridor.Width >= corridor.Height;

            int served = 0;
            int first = int.MaxValue;
            int last = int.MinValue;

            foreach (WallSpan door in plan.Doors)
            {
                int other = -1;

                foreach (WallKey key in door.Keys)
                {
                    int beforeX = key.Vertical ? key.X - 1 : key.X;
                    int beforeY = key.Vertical ? key.Y : key.Y - 1;

                    int before = map.At(beforeX, beforeY);
                    int after = map.At(key.X, key.Y);

                    if (before != hallIndex && after != hallIndex)
                        continue;

                    other = before == hallIndex ? after : before;

                    int along = before == hallIndex
                        ? (alongX ? beforeX : beforeY)
                        : (alongX ? key.X : key.Y);

                    first = Math.Min(first, along);
                    last = Math.Max(last, along);
                }

                if (other >= 0 && other != CellMap.Outside
                    && plan.Rooms[other].Type != RoomType.Living)
                    served++;
            }

            int livingIndex = plan.Rooms.IndexOf(plan.Find(RoomType.Living));
            bool merging = served < s.MinCorridorRooms;

            if (merging || plan.Openings.Count > 0)
                for (int x = corridor.X; x < corridor.Right; x++)
                    for (int y = corridor.Y; y < corridor.Bottom; y++)
                        if (map.At(x - 1, y) == livingIndex || map.At(x + 1, y) == livingIndex
                            || map.At(x, y - 1) == livingIndex || map.At(x, y + 1) == livingIndex)
                        {
                            int along = alongX ? x : y;
                            first = Math.Min(first, along);
                            last = Math.Max(last, along);
                        }

            bool changed = false;

            if (first <= last)
            {
                int start = alongX ? corridor.X : corridor.Y;
                int end = alongX ? corridor.Right : corridor.Bottom;

                if (first - start >= s.MinCorridorCut)
                    changed |= CutEnd(plan, hall, alongX, true, first - start);

                if (end - 1 - last >= s.MinCorridorCut)
                    changed |= CutEnd(plan, hall, alongX, false, end - 1 - last);
            }

            if (merging)
                changed |= MergeIntoLiving(plan, hall);

            return changed;
        }

        private static bool MergeIntoLiving(FloorPlan plan, Room hall)
        {
            Room living = plan.Find(RoomType.Living);

            if (living == null)
                return false;

            foreach (Area part in hall.Parts)
                living.Absorb(part);

            plan.Rooms.Remove(hall);

            return true;
        }

        private static bool CutEnd(FloorPlan plan, Room hall, bool alongX, bool atStart, int cut)
        {
            Area corridor = hall.Parts[0];
            int length = alongX ? corridor.Width : corridor.Height;

            Area piece;
            Area rest;

            if (atStart)
                corridor.Split(alongX, cut, out piece, out rest);
            else
                corridor.Split(alongX, length - cut, out rest, out piece);

            Room receiver = CorridorTrimmer.FindReceiver(plan, piece, alongX);

            if (receiver == null)
                return false;

            hall.Parts[0] = rest;
            receiver.Absorb(piece);

            return true;
        }
    }
}