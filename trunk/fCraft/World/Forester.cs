﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Based on Forester script by dudecon, ported with permission.
// Original: http://www.minecraftforum.net/viewtopic.php?f=25&t=9426
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {

    /// <summary>
    /// Vegetation generator for MapGenerator
    /// </summary>
    sealed class Forester {

        const int MaxTries = 1000;

        // TODO: Add a UI to ConfigTool.AddWorldPopup to set these
        public class ForesterArgs {
            public ForesterOperation Operation = ForesterOperation.Replant;
            public int Treecount = 15; // 0 = no limit if op=conserve/replant
            public TreeShape Shape = TreeShape.Procedural;
            public int Height = 25;
            public int Heightvariation = 15;
            public bool Wood = true;
            public float Trunkthickness = 1;
            public float Trunkheight = .7f;
            public float Branchdensity = 1;
            public RootMode Roots = RootMode.Normal;
            public bool Rootbuttresses = true;
            public bool Foliage = true;
            public float Foliagedensity = 1;
            public bool Mapheightlimit = true;
            public Block Planton = Block.Grass;
            public Random Rand;
            public Map InMap;
            public Map OutMap;

            public Block GroundSurfaceBlock = Block.Grass;
            public Block TrunkBlock = Block.Log;
            public Block FoliageBlock = Block.Leaves;

            public void Validate() {
                if( Treecount < 0 ) Treecount = 0;
                if( Height < 1 ) Height = 1;
                if( Heightvariation > Height ) Heightvariation = Height;
                if( Trunkthickness < 0 ) Trunkthickness = 0;
                if( Trunkheight < 0 ) Trunkheight = 0;
                if( Foliagedensity < 0 ) Foliagedensity = 0;
                if( Branchdensity < 0 ) Branchdensity = 0;
            }
        }


        class Tree {
            public Vector3i Pos;
            public int Height = 1;
            public ForesterArgs Args;

            public virtual void Prepare() { }

            public virtual void MakeTrunk() { }

            public virtual void MakeFoliage() { }

            public void Copy( Tree other ) {
                Args = other.Args;
                Pos = other.Pos;
                Height = other.Height;
            }
        }


        class StickTree : Tree {
            public override void MakeTrunk() {
                for( int i = 0; i < Height; i++ ) {
                    Args.OutMap.SetBlock( Pos.X, Pos.Z, Pos.Y + i, Args.TrunkBlock );
                }
            }
        }


        sealed class NormalTree : StickTree {
            public override void MakeFoliage() {
                int topy = Pos[1] + Height - 1;
                int start = topy - 2;
                int end = topy + 2;

                for( int y = start; y < end; y++ ) {
                    int rad;
                    if( y > start + 1 ) {
                        rad = 1;
                    } else {
                        rad = 2;
                    }
                    for( int xoff = -rad; xoff < rad + 1; xoff++ ) {
                        for( int zoff = -rad; zoff < rad + 1; zoff++ ) {
                            if( Args.Rand.NextDouble() > .618 &&
                                Math.Abs( xoff ) == Math.Abs( zoff ) &&
                                Math.Abs( xoff ) == rad ) {
                                continue;
                            }
                            Args.OutMap.SetBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        sealed class BambooTree : StickTree {
            public override void MakeFoliage() {
                int start = Pos[1];
                int end = start + Height + 1;
                for( int y = start; y < end; y++ ) {
                    for( int i = 0; i < 2; i++ ) {
                        int xoff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        int zoff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        Args.OutMap.SetBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                    }
                }
            }
        }


        sealed class PalmTree : StickTree {
            public override void MakeFoliage() {
                int y = Pos[1] + Height;
                for( int xoff = -2; xoff < 3; xoff++ ) {
                    for( int zoff = -2; zoff < 3; zoff++ ) {
                        if( Math.Abs( xoff ) == Math.Abs( zoff ) ) {
                            Args.OutMap.SetBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        class ProceduralTree : Tree {

            public float trunkRadius { get; set; }
            public float branchSlope { get; set; }
            public float trunkHeight { get; set; }
            public float branchDensity { get; set; }
            public float[] foliageShape { get; set; }
            public Vector3i[] foliageCoords { get; set; }


            void CrossSection( Vector3i center, float radius, int diraxis, Block matidx ) {
                int rad = (int)(radius + .618);
                int secidx1 = (diraxis - 1) % 3;
                int secidx2 = (diraxis + 1) % 3;

                Vector3i coord = new Vector3i();

                for( int off1 = -rad; off1 <= rad; off1++ ) {
                    for( int off2 = -rad; off2 <= rad; off2++ ) {
                        float thisdist = (float)Math.Sqrt( Sqr( Math.Abs( off1 ) + .5 ) +
                                                           Sqr( Math.Abs( off2 ) + .5 ) );
                        if( thisdist > radius ) continue;
                        int pri = center[diraxis];
                        int sec1 = center[secidx1] + off1;
                        int sec2 = center[secidx2] + off2;
                        coord[diraxis] = pri;
                        coord[secidx1] = sec1;
                        coord[secidx2] = sec2;
                        Args.OutMap.SetBlock( coord, matidx );
                    }
                }
            }

            public virtual float ShapeFunc( int y ) {
                if( Args.Rand.NextDouble() < 100f / Sqr( Height ) && y < trunkHeight ) {
                    return Height * .12f;
                } else {
                    return -1;
                }
            }

            void FoliageCluster( Vector3i center ) {
                int y = center[1];
                foreach( float i in foliageShape ) {
                    CrossSection( new Vector3i( center[0], center[2], y ), i, 1, Args.FoliageBlock );
                    y++;
                }
            }

            bool TaperedLimb( Vector3i start, Vector3i end, float startSize, float endSize ) {
                Vector3i delta = end - start;
                int primidx = delta.GetLargestComponent();
                int maxdist = delta[primidx];
                if( maxdist == 0 ) return false;
                int primsign = (maxdist > 0 ? 1 : -1);

                int secidx1 = (primidx - 1) % 3;
                int secidx2 = (primidx + 1) % 3;

                int secdelta1 = delta[secidx1];
                float secfac1 = secdelta1 / (float)delta[primidx];
                int secdelta2 = delta[secidx2];
                float secfac2 = secdelta2 / (float)delta[primidx];

                Vector3i coord = new Vector3i();
                int endoffset = delta[primidx] + primsign;

                for( int primoffset = 0; primoffset < endoffset; primoffset += primsign ) {
                    int primloc = start[primidx] + primoffset;
                    int secloc1 = (int)(start[secidx1] + primoffset * secfac1);
                    int secloc2 = (int)(start[secidx2] + primoffset * secfac2);
                    coord[primidx] = primloc;
                    coord[secidx1] = secloc1;
                    coord[secidx2] = secloc2;
                    float primdist = Math.Abs( delta[primidx] );
                    float radius = endSize + (startSize - endSize) * Math.Abs( delta[primidx] - primoffset ) / primdist;

                    CrossSection( coord, radius, primidx, Args.TrunkBlock );
                }
                return true;
            }

            public override void MakeFoliage() {
                foreach( Vector3i coord in foliageCoords ) {
                    FoliageCluster( coord );
                }
                foreach( Vector3i coord in foliageCoords ) {
                    Args.OutMap.SetBlock( coord, Args.FoliageBlock );
                }
            }

            void MakeBranches() {
                int topy = Pos[1] + (int)(trunkHeight + .5);
                float endrad = trunkRadius * (1 - trunkHeight / Height);
                if( endrad < 1 ) endrad = 1;

                foreach( Vector3i coord in foliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord.X - Pos.X ) + Sqr( coord.Z - Pos.Z ) );
                    float ydist = coord[1] - Pos[1];
                    float value = (branchDensity * 220 * Height) / Cub( ydist + dist );

                    if( value < Args.Rand.NextDouble() ) continue;

                    int posy = coord[1];
                    float slope = (float)(branchSlope + (.5 - Args.Rand.NextDouble()) * .16);

                    float branchy, basesize;
                    if( coord[1] - dist * slope > topy ) {
                        float threshold = 1 / (float)Height;
                        if( Args.Rand.NextDouble() < threshold ) continue;
                        branchy = topy;
                        basesize = endrad;
                    } else {
                        branchy = posy - dist * slope;
                        basesize = endrad + (trunkRadius - endrad) *
                                   (topy - branchy) / trunkHeight;
                    }

                    float startsize = (float)(basesize * (1 + Args.Rand.NextDouble()) *
                                              .618 * Math.Pow( dist / Height, .618 ));
                    float rndr = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * basesize * .618);
                    float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                    int rndz = (int)(rndr * Math.Cos( rndang ) + .5);
                    Vector3i startcoord = new Vector3i {
                        X = Pos[0] + rndx,
                        Z = Pos[2] + rndz,
                        Y = (int)branchy
                    };
                    if( startsize < 1 ) startsize = 1;
                    const float endsize = 1;
                    TaperedLimb( startcoord, coord, startsize, endsize );
                }
            }

            struct RootBase {
                public int x, z;
                public float radius;
            }

            void MakeRoots( RootBase[] rootbases ) {
                if( rootbases.Length == 0 ) return;
                foreach( Vector3i coord in foliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord[0] - Pos[0] ) + Sqr( coord[2] - Pos[2] ) );
                    float ydist = coord[1] - Pos[1];
                    float value = (branchDensity * 220 * Height) / Cub( ydist + dist );
                    if( value < Args.Rand.NextDouble() ) continue;

                    RootBase rootbase = rootbases[Args.Rand.Next( 0, rootbases.Length )];
                    int rootx = rootbase.x;
                    int rootz = rootbase.z;
                    float rootbaseradius = rootbase.radius;

                    float rndr = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * rootbaseradius * .618);
                    float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                    int rndz = (int)(rndr * Math.Cos( rndang ) + .5);
                    int rndy = (int)(Args.Rand.NextDouble() * rootbaseradius * .5);
                    Vector3i startcoord = new Vector3i {
                        X = rootx + rndx,
                        Z = rootz + rndz,
                        Y = Pos[1] + rndy
                    };
                    Vector3f offset = new Vector3f( startcoord - coord );

                    if( Args.Shape == TreeShape.Mangrove ) {
                        offset = offset * 1.618f - 1.5f;
                    }

                    Vector3i endcoord = startcoord + new Vector3i( offset );
                    float rootstartsize = (float)(rootbaseradius * .618 * Math.Abs( offset[1] ) / (Height * .618));

                    if( rootstartsize < 1 ) rootstartsize = 1;
                    const float endsize = 1;

                    if( Args.Roots == RootMode.ToStone ||
                        Args.Roots == RootMode.Hanging ) {
                        float offlength = offset.GetLength();
                        if( offlength < 1 ) continue;
                        float rootmid = endsize;
                        Vector3f vec = offset / offlength;

                        Block searchIndex = Block.Air;
                        if( Args.Roots == RootMode.ToStone ) {
                            searchIndex = Block.Stone;
                        } else if( Args.Roots == RootMode.Hanging ) {
                            searchIndex = Block.Air;
                        }

                        int startdist = (int)(Args.Rand.NextDouble() * 6 * Math.Sqrt( rootstartsize ) + 2.8);
                        Vector3i searchstart = new Vector3i( startcoord + vec * startdist );

                        dist = startdist + DistanceToBlock( Args.InMap, new Vector3f( searchstart ), vec, searchIndex );

                        if( dist < offlength ) {
                            rootmid += (rootstartsize - endsize) * (1 - dist / offlength);
                            endcoord = new Vector3i( startcoord + vec * dist );
                            if( Args.Roots == RootMode.Hanging ) {
                                float remaining_dist = offlength - dist;
                                Vector3i bottomcord = endcoord;
                                bottomcord[1] -= (int)remaining_dist;
                                TaperedLimb( endcoord, bottomcord, rootmid, endsize );
                            }
                        }
                        TaperedLimb( startcoord, endcoord, rootstartsize, rootmid );
                    } else {
                        TaperedLimb( startcoord, endcoord, rootstartsize, endsize );
                    }
                }
            }

            public override void MakeTrunk() {
                int starty = Pos[1];
                int midy = (int)(Pos[1] + trunkHeight * .382);
                int topy = (int)(Pos[1] + trunkHeight + .5);

                int x = Pos[0];
                int z = Pos[2];
                float midrad = trunkRadius * .8f;
                float endrad = trunkRadius * (1 - trunkHeight / Height);

                if( endrad < 1 ) endrad = 1;
                if( midrad < endrad ) midrad = endrad;

                float startrad;
                List<RootBase> rootbases = new List<RootBase>();
                if( Args.Rootbuttresses || Args.Shape == TreeShape.Mangrove ) {
                    startrad = trunkRadius * .8f;
                    rootbases.Add( new RootBase {
                        x = x,
                        z = z,
                        radius = startrad
                    } );
                    float buttress_radius = trunkRadius * .382f;
                    float posradius = trunkRadius;
                    if( Args.Shape == TreeShape.Mangrove ) {
                        posradius *= 2.618f;
                    }
                    int num_of_buttresss = (int)(Math.Sqrt( trunkRadius ) + 3.5);
                    for( int i = 0; i < num_of_buttresss; i++ ) {
                        float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        float thisposradius = (float)(posradius * (.9 + Args.Rand.NextDouble() * .2));
                        int thisx = x + (int)(thisposradius * Math.Sin( rndang ));
                        int thisz = z + (int)(thisposradius * Math.Cos( rndang ));

                        float thisbuttressradius = (float)(buttress_radius * (.618 + Args.Rand.NextDouble()));
                        if( thisbuttressradius < 1 ) thisbuttressradius = 1;

                        TaperedLimb( new Vector3i( thisx, thisz, starty ), new Vector3i( x, z, midy ),
                                     thisbuttressradius, thisbuttressradius );
                        rootbases.Add( new RootBase {
                            x = thisx,
                            z = thisz,
                            radius = thisbuttressradius
                        } );
                    }
                } else {
                    startrad = trunkRadius;
                    rootbases.Add( new RootBase {
                        x = x,
                        z = z,
                        radius = startrad
                    } );
                }
                TaperedLimb( new Vector3i( x, z, starty ), new Vector3i( x, z, midy ), startrad, midrad );
                TaperedLimb( new Vector3i( x, z, midy ), new Vector3i( x, z, topy ), midrad, endrad );
                MakeBranches();
                if( Args.Roots != RootMode.None ) {
                    MakeRoots( rootbases.ToArray() );
                }
            }

            public override void Prepare() {
                base.Prepare();
                trunkRadius = (float)Math.Sqrt( Height * Args.Trunkthickness );
                if( trunkRadius < 1 ) trunkRadius = 1;

                trunkHeight = Height * .618f;
                branchDensity = (Args.Branchdensity / Args.Foliagedensity);

                int ystart = Pos[1];
                int yend = (Pos[1] + Height);
                int num_of_clusters_per_y = (int)(1.5 + Sqr( Args.Foliagedensity * Height / 19f ));
                if( num_of_clusters_per_y < 1 ) num_of_clusters_per_y = 1;

                List<Vector3i> _foliageCoords = new List<Vector3i>();
                for( int y = yend - 1; y >= ystart; y-- ) {
                    for( int i = 0; i < num_of_clusters_per_y; i++ ) {
                        float shapefac = ShapeFunc( y - ystart );
                        if( shapefac < 0 ) continue;
                        float r = (float)((Math.Sqrt( Args.Rand.NextDouble() ) + .328) * shapefac);
                        float theta = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        int x = (int)(r * Math.Sin( theta )) + Pos[0];
                        int z = (int)(r * Math.Cos( theta )) + Pos[2];
                        _foliageCoords.Add( new Vector3i( x, z, y ) );
                    }
                }
                foliageCoords = _foliageCoords.ToArray();
            }
        }


        class RoundTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                branchSlope = .382f;
                foliageShape = new[] { 2, 3, 3, 2.5f, 1.6f };
                trunkRadius *= .8f;
                trunkHeight = Args.Trunkheight * Height;
            }

            public override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;

                if( y < Height * (.282 + .1 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }

                float radius = Height / 2f;
                float adj = Height / 2f - y;
                float dist;
                if( adj == 0 ) {
                    dist = radius;
                } else if( Math.Abs( adj ) >= radius ) {
                    dist = 0;
                } else {
                    dist = (float)Math.Sqrt( radius * radius - adj * adj );
                }
                dist *= .618f;
                return dist;
            }
        }


        sealed class ConeTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                branchSlope = .15f;
                foliageShape = new[] { 3, 2.6f, 2, 1 };
                trunkRadius *= .618f;
                trunkHeight = Height;
            }

            public override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;
                if( y < Height * (.25 + .05 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }
                float radius = (Height - y) * .382f;
                if( radius < 0 ) radius = 0;
                return radius;
            }
        }


        sealed class RainforestTree : ProceduralTree {
            public override void Prepare() {
                foliageShape = new[] { 3.4f, 2.6f };
                base.Prepare();
                branchSlope = 1;
                trunkRadius *= .382f;
                trunkHeight = Height * .9f;
            }

            public override float ShapeFunc( int y ) {
                if( y < Height * .8 ) {
                    if( Args.Height < Height ) {
                        float twigs = base.ShapeFunc( y );
                        if( twigs >= 0 && Args.Rand.NextDouble() < .05 ) {
                            return twigs;
                        }
                    }
                    return -1;
                } else {
                    float width = Height * .382f;
                    float topdist = (Height - y) / (Height * .2f);
                    float dist = (float)(width * (.618 + topdist) * (.618 + Args.Rand.NextDouble()) * .382);
                    return dist;
                }
            }
        }


        sealed class MangroveTree : RoundTree {
            public override void Prepare() {
                base.Prepare();
                branchSlope = 1;
                trunkRadius *= .618f;
            }

            public override float ShapeFunc( int y ) {
                float val = base.ShapeFunc( y );
                if( val < 0 ) return -1;
                val *= 1.618f;
                return val;
            }
        }


        public enum ForesterOperation {
            ClearCut,
            Conserve,
            Replant,
            Add
        }

        public enum TreeShape {
            Normal,
            Bamboo,
            Palm,
            Stickly,
            Round,
            Cone,
            Procedural,
            Rainforest,
            Mangrove
        }

        public enum RootMode {
            Normal,
            ToStone,
            Hanging,
            None
        }


        ForesterArgs args;

        public Forester( ForesterArgs _args ) {
            args = _args;
            args.Validate();
        }

        public static int DistanceToBlock( Map map, Vector3f coord, Vector3f vec, Block blockType ) {
            return DistanceToBlock( map, coord, vec, blockType, false );
        }

        public static int DistanceToBlock( Map map, Vector3f coord, Vector3f vec, Block blockType, bool invert ) {
            coord += .5f;
            int iterations = 0;
            while( map.InBounds( new Vector3i( coord ) ) ) {
                byte blockAtPos = map.GetBlock( new Vector3i( coord ) );
                if( (blockAtPos == (byte)blockType && !invert) ||
                    (blockAtPos != (byte)blockType && invert) ) {
                    break;
                } else {
                    coord += vec;
                    iterations++;
                }
            }
            return iterations;
        }

        void FindTrees( List<Tree> treelist ) {
            int treeheight = args.Height;

            for( int x = 0; x < args.InMap.WidthX; x++ ) {
                for( int z = 0; z < args.InMap.WidthY; z++ ) {
                    int y = args.InMap.Height - 1;
                    while( true ) {
                        int foliagetop = args.InMap.SearchColumn( x, z, args.FoliageBlock, y );
                        if( foliagetop < 0 ) break;
                        y = foliagetop;
                        Vector3i trunktop = new Vector3i( x, z, y - 1 );
                        int height = DistanceToBlock( args.InMap, new Vector3f( trunktop ), new Vector3f( 0, 0, -1 ), args.TrunkBlock, true );
                        if( height == 0 ) {
                            y--;
                            continue;
                        }
                        y -= height;
                        if( args.Height > 0 ) {
                            height = args.Rand.Next( treeheight - args.Heightvariation,
                                                     treeheight + args.Heightvariation + 1 );
                        }
                        treelist.Add( new Tree {
                            Args = args,
                            Pos = new Vector3i( x, z, y ),
                            Height = height
                        } );
                        y--;
                    }
                }
            }
        }

        void PlantTrees( List<Tree> treelist ) {
            int treeheight = args.Height;

            int attempts = 0;
            while( treelist.Count < args.Treecount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeheight - args.Heightvariation,
                                             treeheight + args.Heightvariation + 1 );

                Vector3i treeLoc = RandomTreeLoc( height );
                if( treeLoc.Y < 0 ) continue;
                else treeLoc.Y++;
                treelist.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = treeLoc
                } );
            }
        }

        Vector3i RandomTreeLoc( int height ) {
            int padding = (int)(height / 3f + 1);
            int mindim = Math.Min( args.InMap.WidthX, args.InMap.WidthY );
            if( padding > mindim / 2.2 ) {
                padding = (int)(mindim / 2.2);
            }
            int x = args.Rand.Next( padding, args.InMap.WidthX - padding - 1 );
            int z = args.Rand.Next( padding, args.InMap.WidthY - padding - 1 );
            int y = args.InMap.SearchColumn( x, z, args.Planton );
            return new Vector3i( x, z, y );
        }

        void PlantRainForestTrees( List<Tree> treelist ) {
            int treeheight = args.Height;

            int existingtreenum = treelist.Count;
            int remainingtrees = args.Treecount - existingtreenum;

            const int short_tree_fraction = 6;
            int attempts = 0;
            for( int i = 0; i < remainingtrees && attempts < MaxTries; attempts++ ) {
                float randomfac = (float)((Math.Sqrt( args.Rand.NextDouble() ) * 1.618 - .618) * args.Heightvariation + .5);

                int height;
                if( i % short_tree_fraction == 0 ) {
                    height = (int)(treeheight + randomfac);
                } else {
                    height = (int)(treeheight - randomfac);
                }
                Vector3i xyz = RandomTreeLoc( height );
                if( xyz.Y < 0 ) continue;

                xyz.Y++;

                bool displaced = false;
                foreach( Tree otherTree in treelist ) {
                    Vector3i otherLoc = otherTree.Pos;
                    float otherheight = otherTree.Height;
                    int tallx = otherLoc[0];
                    int tallz = otherLoc[2];
                    float dist = (float)Math.Sqrt( Sqr( tallx - xyz.X + .5 ) + Sqr( tallz - xyz.Z + .5 ) );
                    float threshold = (otherheight + height) * .193f;
                    if( dist < threshold ) {
                        displaced = true;
                        break;
                    }
                }
                if( displaced ) continue;
                treelist.Add( new RainforestTree {
                    Args = args,
                    Pos = xyz,
                    Height = height
                } );
                i++;
            }
        }

        void PlantMangroves( List<Tree> treelist ) {
            int treeheight = args.Height;

            int attempts = 0;
            while( treelist.Count < args.Treecount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeheight - args.Heightvariation,
                                             treeheight + args.Heightvariation + 1 );
                int padding = (int)(height / 3f + 1);
                int mindim = Math.Min( args.InMap.WidthX, args.InMap.WidthY );
                if( padding > mindim / 2.2 ) {
                    padding = (int)(mindim / 2.2);
                }
                int x = args.Rand.Next( padding, args.InMap.WidthX - padding - 1 );
                int z = args.Rand.Next( padding, args.InMap.WidthY - padding - 1 );
                int top = args.InMap.Height - 1;

                int y = top - DistanceToBlock( args.InMap, new Vector3f( x, z, top ), new Vector3f( 0, 0, -1 ), Block.Air, true );
                int dist = DistanceToBlock( args.InMap, new Vector3f( x, z, y ), new Vector3f( 0, 0, -1 ), Block.Water, true );

                if( dist > height * .618 || dist == 0 ) {
                    continue;
                }

                y += (int)Math.Sqrt( height - dist ) + 2;
                treelist.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = new Vector3i( x, z, y )
                } );
            }
        }

        void ProcessTrees( List<Tree> treelist ) {
            TreeShape[] shape_choices;
            switch( args.Shape ) {
                case TreeShape.Stickly:
                    shape_choices = new[]{ TreeShape.Normal,
                                                     TreeShape.Bamboo,
                                                     TreeShape.Palm};
                    break;
                case TreeShape.Procedural:
                    shape_choices = new[]{ TreeShape.Round,
                                                     TreeShape.Cone };
                    break;
                default:
                    shape_choices = new[] { args.Shape };
                    break;
            }

            for( int i = 0; i < treelist.Count; i++ ) {
                TreeShape newshape = shape_choices[args.Rand.Next( 0, shape_choices.Length )];
                Tree newtree;
                switch( newshape ) {
                    case TreeShape.Normal:
                        newtree = new NormalTree();
                        break;
                    case TreeShape.Bamboo:
                        newtree = new BambooTree();
                        break;
                    case TreeShape.Palm:
                        newtree = new PalmTree();
                        break;
                    case TreeShape.Round:
                        newtree = new RoundTree();
                        break;
                    case TreeShape.Cone:
                        newtree = new ConeTree();
                        break;
                    case TreeShape.Rainforest:
                        newtree = new RainforestTree();
                        break;
                    case TreeShape.Mangrove:
                        newtree = new MangroveTree();
                        break;
                    default:
                        throw new ArgumentException("Unknown tree shape type");
                }
                newtree.Copy( treelist[i] );

                if( args.Mapheightlimit ) {
                    int height = newtree.Height;
                    int ybase = newtree.Pos[1];
                    int mapheight = args.InMap.Height;
                    int foliageheight;
                    if( args.Shape == TreeShape.Rainforest ) {
                        foliageheight = 2;
                    } else {
                        foliageheight = 4;
                    }
                    if( ybase + height + foliageheight > mapheight ) {
                        newtree.Height = mapheight - ybase - foliageheight;
                    }
                }

                if( newtree.Height < 1 ) newtree.Height = 1;
                newtree.Prepare();
                treelist[i] = newtree;
            }
        }

        public void Generate() {
            List<Tree> treelist = new List<Tree>();

            if( args.Operation == ForesterOperation.Conserve ) {
                FindTrees( treelist );
            }

            if( args.Treecount > 0 && treelist.Count > args.Treecount ) {
                treelist = treelist.Take( args.Treecount ).ToList();
            }

            if( args.Operation == ForesterOperation.Replant || args.Operation == ForesterOperation.Add ) {
                switch( args.Shape ) {
                    case TreeShape.Rainforest:
                        PlantRainForestTrees( treelist );
                        break;
                    case TreeShape.Mangrove:
                        PlantMangroves( treelist );
                        break;
                    default:
                        PlantTrees( treelist );
                        break;
                }
            }

            if( args.Operation != ForesterOperation.ClearCut ) {
                ProcessTrees( treelist );
                if( args.Foliage ) {
                    foreach( Tree tree in treelist ) {
                        tree.MakeFoliage();
                    }
                }
                if( args.Wood ) {
                    foreach( Tree tree in treelist ) {
                        tree.MakeTrunk();
                    }
                }
            }
        }

        public static float Sqr( float val ) {
            return val * val;
        }
        public static float Cub( float val ) {
            return val * val * val;
        }
        public static int Sqr( int val ) {
            return val * val;
        }
        public static int Cub( int val ) {
            return val * val * val;
        }
        public static double Sqr( double val ) {
            return val * val;
        }
        public static double Cub( double val ) {
            return val * val * val;
        }
    }
}