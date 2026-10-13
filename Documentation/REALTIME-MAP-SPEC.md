# Real-Time Instructor Map - Technical Specification

## Feature Summary

A live map where students see available driving instructors as car markers on OpenStreetMap (Leaflet). Instructors broadcast their GPS position in real time via SignalR. Students click a marker to view details and book instantly. Booked or offline instructors vanish from the map for all viewers.

---

## Architecture

```
+---------------------------+          +---------------------------+
|   Instructor Browser      |          |    Student Browser        |
|                           |          |                           |
|  Geolocation API (GPS)    |          |  Leaflet + OpenStreetMap  |
|  SignalR JS Client        |          |  SignalR JS Client        |
|  "Go Live" / "Go Offline" |          |  Click marker = Book      |
+------------+--------------+          +------------+--------------+
             |                                      ^
             | WebSocket (SignalR)                   | WebSocket (SignalR)
             v                                      |
+---------------------------------------------------------------+
|                   ASP.NET Core Backend                         |
|                                                               |
|  Program.cs (SignalR + CORS config)                           |
|  Hubs/InstructorHub.cs (real-time relay)                      |
|  Controllers/InstructorsController.cs (BookRealTime action)   |
|  Data/ApplicationDbContext.cs (EF Core)                       |
+---------------------------------------------------------------+
             |
             v
+---------------------------+
|   SQL Server (LocalDB)    |
|   Bookings, Instructors   |
+---------------------------+
```

---

## File Structure (new files)

```
Backend/
├── Hubs/
│   └── InstructorHub.cs            # SignalR hub for location broadcasting
├── Models/
│   └── InstructorLocation.cs       # Lightweight DTO for map data
├── Controllers/
│   └── InstructorsController.cs    # + BookRealTime action (existing file)
├── Views/
│   ├── Instructors/
│   │   └── LiveMap.cshtml          # Student-facing live map page
│   └── InstructorDashboard/
│       └── GoLive.cshtml           # Instructor "Go Live" toggle page
├── wwwroot/
│   └── js/
│       └── livemap.js              # SignalR + Leaflet map logic
└── Program.cs                      # + AddSignalR(), MapHub<InstructorHub>()
```

---

## Data Flow

### Instructor Goes Live

```
1. Instructor opens /InstructorDashboard/GoLive
2. Clicks "Go Live" button
3. Browser requests GPS permission (Geolocation API)
4. On success, connects to SignalR hub (/instructorHub)
5. Every 10 seconds:
   - Browser reads navigator.geolocation.getCurrentPosition()
   - Sends to hub: UpdateLocation(instructorId, name, lat, lng, vehicleType)
6. Hub broadcasts to all clients: "InstructorLocationUpdated"
```

### Student Views Map

```
1. Student opens /Instructors/LiveMap
2. Page loads Leaflet map centered on student's location
3. Connects to SignalR hub (/instructorHub)
4. Listens for "InstructorLocationUpdated":
   - Adds or moves marker on map
   - Marker shows car icon + instructor name tooltip
5. Listens for "InstructorRemoved":
   - Removes marker from map
```

### Student Books from Map

```
1. Student clicks instructor marker
2. Popup shows: name, vehicle type (Manual/Automatic), experience, "Book Now" button
3. Student clicks "Book Now"
4. POST /Instructors/BookRealTime (instructorId, studentId)
5. Server creates Booking (status: Pending)
6. Server calls hub: BroadcastInstructorRemoved(instructorId)
7. All connected students see that marker disappear instantly
8. Instructor receives notification (TempData or SignalR event)
```

### Instructor Goes Offline

```
1. Instructor clicks "Go Offline" button
2. Sends to hub: GoOffline(instructorId)
3. Hub broadcasts: "InstructorRemoved"
4. Marker disappears for all students
5. SignalR connection closed

Alternative: instructor closes browser tab
   - Hub fires OnDisconnectedAsync()
   - Broadcasts "InstructorRemoved" automatically
```

---

## SignalR Hub Methods

| Method | Called By | Broadcasts | Description |
|--------|-----------|-----------|-------------|
| UpdateLocation | Instructor | InstructorLocationUpdated | Sends GPS coords to all students |
| GoOffline | Instructor | InstructorRemoved | Removes from all maps |
| BroadcastInstructorRemoved | Server (booking) | InstructorRemoved | Hides booked instructor |
| OnDisconnectedAsync | Auto (framework) | InstructorRemoved | Cleanup on disconnect |

---

## Client Events (JS listeners)

| Event Name | Received By | Action |
|------------|-------------|--------|
| InstructorLocationUpdated | Student | Add/move marker on map |
| InstructorRemoved | Student | Remove marker from map |
| BookingReceived | Instructor | Show notification of new booking |

---

## Models

### InstructorLocation (DTO)

```csharp
public class InstructorLocation
{
    public int InstructorId { get; set; }
    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string VehicleType { get; set; }  // Manual, Automatic
    public bool IsAvailable { get; set; }
}
```

---

## Technology Choices

| Component | Choice | Reason |
|-----------|--------|--------|
| Real-time | SignalR (built into .NET 8) | No extra packages needed server-side |
| Map | Leaflet + OpenStreetMap | Free, no API key required |
| GPS | Browser Geolocation API | Works on mobile and desktop |
| Transport | WebSocket (auto-fallback) | SignalR handles protocol negotiation |

---

## Configuration Changes (Program.cs)

```csharp
// Add SignalR service
builder.Services.AddSignalR();

// Map the hub endpoint (after app.UseRouting())
app.MapHub<InstructorHub>("/instructorHub");
```

No CORS needed since everything runs on the same origin (same ASP.NET app serves both views and the hub).

---

## Navigation Updates

| Role | New Link | Points To |
|------|----------|-----------|
| Student | "Live Map" | /Instructors/LiveMap |
| Instructor | "Go Live" | /InstructorDashboard/GoLive |

---

## Future Enhancements

1. Radius filter (only show instructors within 10km)
2. Custom car icons per vehicle type (Manual = blue, Automatic = green)
3. Smooth marker animation (interpolate between GPS pings)
4. Instructor rating badge on popup
5. Mobile push notifications for bookings
6. Store GPS trail history for lesson tracking
