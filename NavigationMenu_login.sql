
/****** Object:  Table [dbo].[Modules]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Modules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ModuleKey] [varchar](50) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Domain] [varchar](250) NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NULL,
	[UpdatedAt] [datetime] NULL,
	[UpdatedBy] [int] NULL,
 CONSTRAINT [PK__Modules__3214EC077AAA6B1D] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__Modules__37DD71913986028B] UNIQUE NONCLUSTERED 
(
	[ModuleKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NavigationMenuApiCodes]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NavigationMenuApiCodes](
	[NavigationMenuId] [int] NOT NULL,
	[ApiCode] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NavigationMenuId] ASC,
	[ApiCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NavigationMenus]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NavigationMenus](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[ClientUrl] [nvarchar](250) NULL,
	[Icon] [nvarchar](128) NULL,
	[ParentId] [int] NULL,
	[ModuleId] [int] NOT NULL,
	[Section] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsHidden] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CreatedBy] [int] NULL,
	[UpdatedAt] [datetime] NULL,
	[UpdatedBy] [int] NULL,
 CONSTRAINT [PK__Navigati__3214EC07DA0C8DAC] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RoleNavigationMenus]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleNavigationMenus](
	[RoleId] [int] NOT NULL,
	[NavigationMenuId] [int] NOT NULL,
	[Permission] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC,
	[NavigationMenuId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[GhiChu] [nvarchar](250) NULL,
	[IsLocked] [bit] NOT NULL,
	[DateCreated] [datetime] NOT NULL,
 CONSTRAINT [PK__Roles__3214EC07D007516A] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__Roles__737584F6D05DD400] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserLogins]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserLogins](
	[Id] [nvarchar](150) NOT NULL,
	[UserId] [int] NOT NULL,
	[LoginTime] [datetime] NOT NULL,
	[ExpiresTime] [datetime] NOT NULL,
	[ExpiresTimeRefresh] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoles]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 9/30/2025 7:47:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](150) NOT NULL,
	[Password] [nvarchar](150) NOT NULL,
	[LoginFailed] [bit] NOT NULL,
	[LastLoginFailed] [datetime] NULL,
	[LockOutDate] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[UpdatePassword] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[CreatedBy] [int] NULL,
	[UpdatedAt] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[CustomerTenantId] [int] NULL,
 CONSTRAINT [PK__Users__3214EC079580025C] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__Users__536C85E4FB5681C0] UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Modules] ADD  CONSTRAINT [DF__Modules__IsLocke__36B12243]  DEFAULT ((0)) FOR [IsLocked]
GO
ALTER TABLE [dbo].[Modules] ADD  CONSTRAINT [DF_Modules_SortOrder]  DEFAULT ((1)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[Modules] ADD  CONSTRAINT [DF_Modules_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Modules] ADD  CONSTRAINT [DF_Modules_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[NavigationMenus] ADD  CONSTRAINT [DF__Navigatio__Secti__66603565]  DEFAULT ((0)) FOR [Section]
GO
ALTER TABLE [dbo].[NavigationMenus] ADD  CONSTRAINT [DF__Navigatio__IsHid__6754599E]  DEFAULT ((0)) FOR [IsHidden]
GO
ALTER TABLE [dbo].[NavigationMenus] ADD  CONSTRAINT [DF_NavigationMenus_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__LoginFail__398D8EEE]  DEFAULT ((0)) FOR [LoginFailed]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__CreatedAt__3A81B327]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__IsLocked__3B75D760]  DEFAULT ((0)) FOR [IsLocked]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__UpdatePas__3C69FB99]  DEFAULT ((0)) FOR [UpdatePassword]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__IsDeleted__3D5E1FD2]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[NavigationMenuApiCodes]  WITH CHECK ADD  CONSTRAINT [FK_Nav_ApiCodes] FOREIGN KEY([NavigationMenuId])
REFERENCES [dbo].[NavigationMenus] ([Id])
GO
ALTER TABLE [dbo].[NavigationMenuApiCodes] CHECK CONSTRAINT [FK_Nav_ApiCodes]
GO
ALTER TABLE [dbo].[NavigationMenus]  WITH CHECK ADD  CONSTRAINT [FK_NavigationMenus_Module] FOREIGN KEY([ModuleId])
REFERENCES [dbo].[Modules] ([Id])
GO
ALTER TABLE [dbo].[NavigationMenus] CHECK CONSTRAINT [FK_NavigationMenus_Module]
GO
ALTER TABLE [dbo].[NavigationMenus]  WITH CHECK ADD  CONSTRAINT [FK_NavigationMenus_Parent] FOREIGN KEY([ParentId])
REFERENCES [dbo].[NavigationMenus] ([Id])
GO
ALTER TABLE [dbo].[NavigationMenus] CHECK CONSTRAINT [FK_NavigationMenus_Parent]
GO
ALTER TABLE [dbo].[RoleNavigationMenus]  WITH CHECK ADD  CONSTRAINT [FK_RoleNav_Nav] FOREIGN KEY([NavigationMenuId])
REFERENCES [dbo].[NavigationMenus] ([Id])
GO
ALTER TABLE [dbo].[RoleNavigationMenus] CHECK CONSTRAINT [FK_RoleNav_Nav]
GO
ALTER TABLE [dbo].[RoleNavigationMenus]  WITH CHECK ADD  CONSTRAINT [FK_RoleNav_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[RoleNavigationMenus] CHECK CONSTRAINT [FK_RoleNav_Roles]
GO
ALTER TABLE [dbo].[UserLogins]  WITH CHECK ADD  CONSTRAINT [FK_UserLogins_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UserLogins] CHECK CONSTRAINT [FK_UserLogins_Users]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users]
GO
