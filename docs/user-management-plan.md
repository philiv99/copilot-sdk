# User Management Implementation Plan

## Overview

This document outlines the implementation plan for adding user management to the Copilot SDK Demo Application. This addresses **Appendix A.2 (No Authentication or Authorization)** from the technical overview.

**Scope**: Local user accounts with three roles (Admin, Creator, Player), no external OAuth or email/phone verification (stubs provided for future integration). No JWT or session tokens — uses a simple cookie-based "current user" approach suitable for a proof-of-concept.

**Design Philosophy**: Since this is a PoC/demo application, we implement "soft auth" — users log in with username/password, the backend tracks the current user, and role-based access is enforced at the API level. There is no cryptographic session management (no JWT, no OAuth). Passwords are hashed with a salt using SHA-256 for basic security.

---

## Data Model

### User Entity

| Field | Type | Constraints |
|-------|------|-------------|
| `Id` | string (GUID) | Primary key |
| `Username` | string | Unique, required, 3-50 chars, alphanumeric + underscore |
| `Email` | string | Unique, required, valid email format |
| `DisplayName` | string | Required, 1-100 chars |
| `PasswordHash` | string | SHA-256 hash with salt |
| `PasswordSalt` | string | Random salt per user |
| `Role` | enum | Admin, Creator, Player |
| `AvatarType` | enum | Default, Preset, Custom |
| `AvatarData` | string | Preset name or base64-encoded image data |
| `IsActive` | bool | Soft delete flag |
| `CreatedAt` | DateTime | UTC timestamp |
| `UpdatedAt` | DateTime | UTC timestamp |
| `LastLoginAt` | DateTime? | UTC timestamp, nullable |

### Default Seed Data

On first run, a default admin account is created:
- Username: `admin`
- Password: `admin123`
- Role: Admin
- Email: `admin@local`

---

## Architecture

### Backend

```
Controllers/
  UserController.cs          — CRUD + auth endpoints
Services/
  IUserService.cs            — Interface
  UserService.cs             — Business logic
Models/
  Domain/
    User.cs                  — Domain entity
    UserRole.cs              — Enum (Admin, Creator, Player)
    AvatarType.cs            — Enum (Default, Preset, Custom)
  Requests/
    RegisterRequest.cs       — New account registration
    LoginRequest.cs          — Login
    UpdateProfileRequest.cs  — Self-profile update
    AdminUpdateUserRequest.cs — Admin user management
    ChangePasswordRequest.cs — Password change
    ForgotUsernameRequest.cs — Forgot username (stub)
    ForgotPasswordRequest.cs — Forgot password (stub)
  Responses/
    UserResponse.cs          — Public user info
    LoginResponse.cs         — Login result with user info
    UserListResponse.cs      — Admin: paginated user list
```

### Frontend

```
types/
  user.types.ts              — All user-related TypeScript types
api/
  userApi.ts                 — User REST API client
context/
  UserContext.tsx             — Auth state management
views/
  LoginView.tsx              — Login form
  RegisterView.tsx           — Registration form
  ProfileView.tsx            — User profile editor
  AdminUsersView.tsx         — Admin user management
  ForgotCredentialsView.tsx  — Forgot username/password
components/
  Auth/
    ProtectedRoute.tsx       — Role-based route guard
    AvatarPicker.tsx         — Avatar selection/upload
    UserMenu.tsx             — Header user dropdown
    RoleBadge.tsx            — Role indicator badge
    UserTable.tsx            — Admin user list table
```

---

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/users/register` | Public | Create new account |
| POST | `/api/users/login` | Public | Authenticate user |
| POST | `/api/users/logout` | Authenticated | Clear current user |
| GET | `/api/users/me` | Authenticated | Get current user profile |
| PUT | `/api/users/me` | Authenticated | Update own profile |
| PUT | `/api/users/me/password` | Authenticated | Change password |
| PUT | `/api/users/me/avatar` | Authenticated | Update avatar |
| GET | `/api/users` | Admin only | List all users |
| GET | `/api/users/{id}` | Admin only | Get user by ID |
| PUT | `/api/users/{id}` | Admin only | Update any user |
| DELETE | `/api/users/{id}` | Admin only | Deactivate user |
| POST | `/api/users/{id}/activate` | Admin only | Reactivate user |
| POST | `/api/users/{id}/reset-password` | Admin only | Reset user password |
| POST | `/api/users/forgot-username` | Public | Forgot username (stub) |
| POST | `/api/users/forgot-password` | Public | Forgot password (stub) |
| GET | `/api/users/avatars/presets` | Public | List preset avatars |

---

## Implementation Phases

### Phase U1: Backend Domain & Persistence

**Tasks:**
- [ ] U1.1 Create `Models/Domain/User.cs` with all fields
- [ ] U1.2 Create `Models/Domain/UserRole.cs` enum
- [ ] U1.3 Create `Models/Domain/AvatarType.cs` enum
- [ ] U1.4 Create all request models in `Models/Requests/`
- [ ] U1.5 Create all response models in `Models/Responses/`
- [ ] U1.6 Add Users table to SQLite schema in `SqlitePersistenceService`
- [ ] U1.7 Add user CRUD methods to `IPersistenceService`
- [ ] U1.8 Implement user persistence in `SqlitePersistenceService`
- [ ] U1.9 Seed default admin account on DB initialization

### Phase U2: Backend Service & Controller

**Tasks:**
- [ ] U2.1 Create `IUserService.cs` interface
- [ ] U2.2 Create `UserService.cs` with password hashing, validation, CRUD
- [ ] U2.3 Create `UserController.cs` with all endpoints
- [ ] U2.4 Add `CurrentUser` middleware/header-based identification
- [ ] U2.5 Register services in `Program.cs`
- [ ] U2.6 Implement forgot username/password stubs

### Phase U3: Backend Tests

**Tasks:**
- [ ] U3.1 Write `UserServiceTests.cs` — registration, login, validation, role checks
- [ ] U3.2 Write `UserControllerTests.cs` — all endpoints, auth checks
- [ ] U3.3 Write user persistence tests in existing persistence test file
- [ ] U3.4 Run all tests and verify no regressions

### Phase U4: Frontend Types & API

**Tasks:**
- [ ] U4.1 Create `types/user.types.ts`
- [ ] U4.2 Create `api/userApi.ts`
- [ ] U4.3 Update `types/index.ts` and `api/index.ts` exports

### Phase U5: Frontend Auth Context & Components

**Tasks:**
- [ ] U5.1 Create `context/UserContext.tsx`
- [ ] U5.2 Create `components/Auth/ProtectedRoute.tsx`
- [ ] U5.3 Create `components/Auth/AvatarPicker.tsx`
- [ ] U5.4 Create `components/Auth/UserMenu.tsx`
- [ ] U5.5 Create `components/Auth/RoleBadge.tsx`
- [ ] U5.6 Create `components/Auth/UserTable.tsx`
- [ ] U5.7 Create `components/Auth/index.ts`

### Phase U6: Frontend Views

**Tasks:**
- [ ] U6.1 Create `views/LoginView.tsx`
- [ ] U6.2 Create `views/RegisterView.tsx`
- [ ] U6.3 Create `views/ProfileView.tsx`
- [ ] U6.4 Create `views/AdminUsersView.tsx`
- [ ] U6.5 Create `views/ForgotCredentialsView.tsx`

### Phase U7: Frontend Integration & Routing

**Tasks:**
- [ ] U7.1 Update `App.tsx` with auth routing (login/register as public, rest protected)
- [ ] U7.2 Update `Header.tsx` with user menu
- [ ] U7.3 Update `MainLayout.tsx` with auth-aware layout
- [ ] U7.4 Update `context/index.ts` with UserContext export
- [ ] U7.5 Update `components/index.ts` with Auth component exports
- [ ] U7.6 Add admin navigation link in layout

### Phase U8: Frontend Tests

**Tasks:**
- [ ] U8.1 Write tests for `UserContext`
- [ ] U8.2 Write tests for `LoginView`
- [ ] U8.3 Write tests for `RegisterView`
- [ ] U8.4 Write tests for `ProfileView`
- [ ] U8.5 Write tests for `AdminUsersView`
- [ ] U8.6 Write tests for Auth components (AvatarPicker, UserMenu, etc.)
- [ ] U8.7 Write tests for `ForgotCredentialsView`
- [ ] U8.8 Run full test suite

---

## Authentication Flow

### Login
1. User submits username + password
2. Backend looks up user by username
3. Verifies password hash matches
4. Returns user info + sets `X-User-Id` response header
5. Frontend stores user in context + localStorage
6. All subsequent API calls include `X-User-Id` header

### Registration
1. User fills form: username, email, display name, password, avatar
2. Backend validates uniqueness (username, email)
3. Creates user with hashed password, default role = Player
4. Returns user info (auto-login after registration)

### Current User Identification
- Frontend sends `X-User-Id` header with every request
- Backend reads this header to identify the current user
- No cryptographic session — this is PoC-grade auth
- If header is missing or invalid, returns 401

### Forgot Username/Password (Stubs)
- **Forgot Username**: Accepts email, returns a message saying "If an account exists with this email, the username has been sent" (but actually does nothing — stub for future email integration)
- **Forgot Password**: Accepts username or email, returns a message saying "If an account exists, a password reset link has been sent" (stub for future email/SMS integration)

---

## Avatar System

### Preset Avatars
A set of 12 built-in avatar options:
- `default` — Generic user silhouette
- `astronaut`, `robot`, `ninja`, `wizard`, `pirate`, `alien`, `cat`, `dog`, `dragon`, `unicorn`, `phoenix`

Stored as emoji/unicode characters for simplicity (no image files needed).

### Custom Avatars
- Users can upload an image (max 256KB, JPEG/PNG)
- Stored as base64 in the `AvatarData` field
- Displayed as a circular thumbnail in the UI

---

## Role Permissions

| Feature | Admin | Creator | Player |
|---------|-------|---------|--------|
| Login/Logout | ✅ | ✅ | ✅ |
| Edit own profile | ✅ | ✅ | ✅ |
| Change own password | ✅ | ✅ | ✅ |
| View sessions | ✅ | ✅ | ✅ |
| Create sessions | ✅ | ✅ | ❌ |
| Send messages | ✅ | ✅ | ✅ |
| Manage client config | ✅ | ❌ | ❌ |
| Manage all users | ✅ | ❌ | ❌ |
| Deactivate/reactivate users | ✅ | ❌ | ❌ |
| Reset other users' passwords | ✅ | ❌ | ❌ |
| Assign roles | ✅ | ❌ | ❌ |

---

## Future Considerations (Stubbed)

1. **Email verification** — `SendVerificationEmail()` method stubbed in UserService
2. **Phone collection** — Schema allows future `Phone` field addition
3. **Password reset via email** — `SendPasswordResetEmail()` stubbed
4. **Username recovery via email** — `SendUsernameReminder()` stubbed
5. **OAuth integration** — Can add external provider fields to User model later
6. **Session tokens / JWT** — Can replace X-User-Id header approach with proper tokens

---

## Progress Tracking

- [x] Phase U1: Backend Domain & Persistence — Completed
- [x] Phase U2: Backend Service & Controller — Completed
- [x] Phase U3: Backend Tests — 398 backend tests passing
- [x] Phase U4: Frontend Types & API — Completed
- [x] Phase U5: Frontend Auth Context & Components — Completed
- [x] Phase U6: Frontend Views — LoginView, RegisterView, ProfileView, AdminUsersView, ForgotCredentialsView
- [x] Phase U7: Frontend Integration & Routing — App.tsx updated with UserProvider, auth routes, ProtectedRoute
- [x] Phase U8: Frontend Tests — 593 frontend tests passing (95 new user management tests)
