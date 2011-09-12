﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public static class BrushManager {
        static readonly Dictionary<string, IBrushFactory> BrushFactories = new Dictionary<string, IBrushFactory>();
        static readonly Dictionary<string, IBrushFactory> BrushAliases = new Dictionary<string, IBrushFactory>();

        static readonly CommandDescriptor CdBrush = new CommandDescriptor {
            Name = "brush",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Gets or sets the current brush. Available brushes are: ",
            HelpSections = new Dictionary<string, string>(),
            Handler = BrushHandler
        };

        static void BrushHandler( Player player, Command cmd ) {
            string brushName = cmd.Next();
            if( brushName == null ) {
                player.Message( player.Brush.Description );
            } else {
                IBrushFactory brushFactory = BrushManager.GetBrushFactory( brushName );
                if( brushFactory == null ) {
                    player.Message( "Unrecognized brush \"{0}\"", brushName );
                } else {
                    IBrush newBrush = brushFactory.MakeBrush( player, cmd );
                    if( newBrush != null ) {
                        player.Brush = newBrush;
                        player.Message( "Brush set to {0}", player.Brush.Description );
                    }
                }
            }
        }

        internal static void Init() {
            CommandManager.RegisterCommand( CdBrush );
            RegisterBrush( NormalBrushFactory.Instance );
            RegisterBrush( CheckeredBrushFactory.Instance );
            RegisterBrush( RandomBrushFactory.Instance );
            RegisterBrush( RainbowBrush.Instance );
            RegisterBrush( CloudyBrushFactory.Instance );
            RegisterBrush( MarbledBrushFactory.Instance );
            RegisterBrush( ReplaceBrushFactory.Instance );
            RegisterBrush( ReplaceNotBrushFactory.Instance );
        }

        public static void RegisterBrush( IBrushFactory factory ) {
            if( factory == null ) throw new ArgumentNullException( "factory" );
            string helpString = factory.Help;
            string lowerName = factory.Name.ToLower();
            BrushFactories.Add( lowerName, factory );
            if( factory.Aliases != null ) {
                helpString += factory.Aliases.JoinToString();
                foreach( string alias in factory.Aliases ) {
                    BrushAliases.Add( alias.ToLower(), factory );
                }
            }
            CdBrush.HelpSections.Add( lowerName, factory.Help );
            CdBrush.Help += factory.Name + " ";
        }

        public static IBrushFactory GetBrushFactory( string brushName ) {
            if( brushName == null ) throw new ArgumentNullException( "brushName" );
            IBrushFactory factory;
            string lowerName = brushName.ToLower();
            if( BrushFactories.TryGetValue( lowerName, out factory ) ||
                BrushAliases.TryGetValue( lowerName, out factory ) ) {
                return factory;
            } else {
                return null;
            }
        }


        static readonly Random Rand = new Random();
        static readonly object RandLock = new object();
        public static int NextSeed() {
            lock( RandLock ) {
                return Rand.Next();
            }
        }
    }
}