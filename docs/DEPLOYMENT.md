# Deployment & Hardware Integration Guide

## Overview

The deployment process involves building, versioning, and provisioning the Beincourt room plugin to hardware (CP4N control processor) via **PD Tools**, the Pepper Dash developer portal.

```
Developer commits code
  ↓
Semantic-release auto-versions (e.g., 2.0.2-csv-zoom-sandbox-v2.1)
  ↓
GitHub Release published with .tgz package
  ↓
PD Tools refreshes version list (1-2 minutes)
  ↓
User selects version in PD Tools UI
  ↓
PD Tools provisions package to CP4N
  ↓
CP4N unpacks, loads, and runs plugin
  ↓
Testing team validates functionality
```

## Target Hardware

### CP4N (Control Processor 4 Series)

**Device Specifications**:
- Model: Cisco Collaboration Endpoint CP4N
- Role: Main control processor for Beincourt courtroom
- Network: 192.168.100.155 (static IP)
- OS: Embedded Linux (customized)
- Plugin Framework: Pepperdash Essentials v2.x

**Access Credentials**:
- Username: `admin`
- Password: [See secure vault / contact admin]
- SSH Port: 22 (standard)

### Alternative Hardware (Testing)

Other potential target devices:
- **CP4C** — Control Processor 4 Series Compact (smaller footprint)
- **CE70** — Control Endpoint CE70 (all-in-one device with control)
- **Zoom Room Appliances** — Direct Zoom Room compute modules

## Deployment via PD Tools

### Prerequisites

1. **PD Tools Account** — Access to Pepper Dash developer portal
2. **Device Provisioning** — Beincourt courtroom registered in PD Tools
3. **Network Connectivity** — CP4N can reach internet for package downloads
4. **Plugin Version Available** — Semantic-release must have completed (wait 1-2 minutes after push)

### Step-by-Step Deployment

#### 1. Navigate to Device Management

```
PD Tools UI → Devices → Beincourt Room (CP4N)
```

#### 2. View Current Plugin Version

```
Active Plugin: epi-bic-zoomroom
Current Version: [displays installed version]
Available Versions: [dropdown list]
```

#### 3. Select Version to Deploy

```
Available Versions ▼
├─ 2.0.2-csv-zoom-sandbox-v2.1  (development)
├─ 2.0.1                        (stable)
├─ 2.0.0                        (stable)
└─ 2.0.8-csv-zoom-sandbox.1     (old pre-release)

Select: 2.0.2-csv-zoom-sandbox-v2.1
```

#### 4. Initiate Deployment

```
Button: "Sync" or "Deploy" or "Update" (label varies by PD Tools version)

Deployment starts immediately
  • Downloads .tgz package from GitHub Release
  • Transfers to CP4N
  • Unpacks and validates
  • Loads plugin into memory
  • Restarts control system (if needed)
```

#### 5. Monitor Progress

```
Deployment Status:
  ⏳ Downloading... (30-60 seconds)
  ⏳ Extracting... (10-20 seconds)
  ⏳ Installing... (30-60 seconds)
  ✅ Deployed Successfully

Timeline: Typically 2-5 minutes total
```

#### 6. Verify Installation

Option A: Check via PD Tools
```
Status: ✅ Online
Plugin: epi-bic-zoomroom@2.0.2-csv-zoom-sandbox-v2.1
Health: ✅ Normal
```

Option B: SSH into CP4N and check
```bash
ssh admin@192.168.100.155

# Check if plugin is loaded
PluginStatus

# Should show:
# epi-bic-zoomroom (v2.0.2-csv-zoom-sandbox-v2.1) - Running
```

## Hardware Testing Procedures

### Pre-Test Checklist

- [ ] Plugin deployed and verified as running (PluginStatus)
- [ ] CP4N has network connectivity (can ping Zoom servers)
- [ ] Zoom meeting URL or meeting ID available for testing
- [ ] Hardware configured with valid Zoom credentials
- [ ] StreamSync device (for annotation) powered on and networked
- [ ] NDI network available (for stream 3 - annotation)

### Test Scenarios

#### Scenario 1: Basic Zoom Room Control

**Objective**: Verify plugin communicates with Zoom Room SDK

**Steps**:
1. SSH into CP4N and check SDK connection:
   ```bash
   ssh admin@192.168.100.155
   # Check Zoom Room status
   show status zCommand
   ```

2. Attempt to join meeting:
   - Via PD Tools or local control UI
   - Zoom meeting URL: [Test meeting URL]
   - Monitor: Does meeting join complete within 10 seconds?

3. Verify meeting state:
   - Participant count matches Zoom
   - Meeting duration counter updates
   - Participant list populated

**Expected Result**: ✅ Join completes, state syncs correctly

---

#### Scenario 2: Presentation Content Sharing (Beincourt-Specific Fix)

**Objective**: Verify StartSharingOnlyMeeting API fix ensures far-end visibility

**Hardware Setup**:
- CP4N in presentation mode (View All layout)
- Second Zoom participant (on laptop/phone)
- Content source (PC, document camera, etc.)

**Steps**:
1. Start Zoom meeting on CP4N
2. Join meeting from second participant (laptop/phone as far-end)
3. Start presentation on CP4N (share screen or document camera)
4. Far-end participant observes:
   - [ ] Presentation content visible in participant feed
   - [ ] Layout includes presenter + content
   - [ ] No "waiting for presenter" message

**Expected Result**: ✅ Content reaches far-end participants (fix working!)

**If Failed**:
- Check logs: SSH to CP4N, grep ZoomRoom logs for shareView commands
- Verify View All layout is active
- Restart plugin and retry

---

#### Scenario 3: Annotation Stream Support

**Objective**: Verify NDI stream 3 carries annotation data when enabled

**Hardware Setup**:
- CP4N connected to network with NDI capability
- StreamSync device online and configured for NDI
- Annotation decoder consuming NDI stream 3

**Steps**:
1. Enable presentation mode annotation
   - Mobile app or control interface
   - Toggle: "Annotation Mode: ON"

2. Verify StreamSync receives stream:
   ```bash
   # On StreamSync or NDI monitor device
   # Check NDI source list
   # Should show: "epi-bic-zoomroom-stream-3"
   ```

3. Verify annotation data:
   - Presenter draws/writes in annotation mode
   - StreamSync displays annotation overlay
   - Latency < 100ms

**Expected Result**: ✅ Annotation feed captured on stream 3

**If Failed**:
- Check NDI network connectivity
- Verify zCommand annotation command sent (see logs)
- Restart StreamSync and retry

---

#### Scenario 4: Camera Control

**Objective**: Pan, tilt, zoom (PTZ) operations work correctly

**Steps**:
1. Start Zoom meeting on CP4N
2. Issue camera commands via control interface:
   - Pan left/right
   - Tilt up/down
   - Zoom in/out

3. Monitor responses:
   - [ ] Commands execute within 1 second
   - [ ] Camera actually moves (visible in far-end video)
   - [ ] No error messages

**Expected Result**: ✅ PTZ operations smooth and responsive

---

#### Scenario 5: Meeting Recording

**Objective**: Record meeting and verify data capture

**Steps**:
1. Start Zoom meeting
2. Initiate recording via control interface or mobile app
3. Verify:
   - Recording indicator appears on CP4N display
   - Zoom shows "This meeting is being recorded"
   - Recording status updates in control system

**Expected Result**: ✅ Recording confirmed and tracked

---

#### Scenario 6: Layout Switching

**Objective**: Switch between Gallery, Speaker, Spotlight layouts

**Steps**:
1. Join Zoom meeting with 3+ participants
2. Switch layouts via control interface:
   - Gallery → Speaker → Spotlight → Custom
3. Verify:
   - [ ] Layout changes within 2 seconds
   - [ ] Participants displayed correctly for each layout
   - [ ] CP4N display matches far-end view

**Expected Result**: ✅ Layouts switch smoothly

---

### Test Execution Checklist

| Scenario | Version Tested | Date | Tester | Result | Notes |
|----------|-----------------|------|--------|--------|-------|
| Basic Control | 2.0.2-csv... | | | ✅/❌ | |
| Presentation Sharing | 2.0.2-csv... | | | ✅/❌ | |
| Annotation Stream | 2.0.2-csv... | | | ✅/❌ | |
| Camera Control | 2.0.2-csv... | | | ✅/❌ | |
| Meeting Recording | 2.0.2-csv... | | | ✅/❌ | |
| Layout Switching | 2.0.2-csv... | | | ✅/❌ | |

## Rollback Procedures

### If Deployment Fails

**Scenario 1: Plugin crashes after deployment**

```bash
# 1. SSH into CP4N
ssh admin@192.168.100.155

# 2. Check plugin status
PluginStatus

# 3. If crashed, restart CP4N
reboot

# 4. After reboot, verify previous version loads
PluginStatus  # Should show last working version
```

**Scenario 2: Revert via PD Tools**

```
PD Tools UI → Devices → Beincourt Room
  ↓
Available Versions ▼ (select previous working version)
  ↓
Button: "Rollback" or "Sync" (with older version)
  ↓
Deployment restarts with previous version
```

### Manual Rollback (Advanced)

If PD Tools is unavailable:

```bash
# SSH into CP4N
ssh admin@192.168.100.155

# Stop current plugin
PluginStop epi-bic-zoomroom

# List available versions
ls /opt/plugins/epi-bic-zoomroom/

# Load previous version (if multiple exist)
PluginStart epi-bic-zoomroom@2.0.1

# Verify
PluginStatus
```

## Monitoring & Logs

### Real-Time Monitoring

**Via PD Tools UI**:
```
Devices → Beincourt Room → Health Dashboard
  • CPU Usage
  • Memory Usage
  • Network Activity
  • Plugin Status
  • Last Sync: [timestamp]
```

**Via SSH**:
```bash
ssh admin@192.168.100.155

# Check process resource usage
ProcStats

# Monitor plugin logs in real-time
tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log

# Check Zoom Room SDK connection
show status zCommand

# Show recent errors
grep -i error /opt/plugins/epi-bic-zoomroom/logs/*.log
```

### Debug Logging

To enable verbose debug output on CP4N:

```bash
# SSH into CP4N
ssh admin@192.168.100.155

# Edit plugin configuration (if available)
vi /opt/plugins/epi-bic-zoomroom/config.json

# Add or modify:
{
  "debugLogging": true,
  "logLevel": "DEBUG"
}

# Restart plugin
PluginStop epi-bic-zoomroom
PluginStart epi-bic-zoomroom

# Tail logs to see debug output
tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log
```

## Performance Tuning

### Connection Timeout Adjustment

If Zoom Room SDK connection is timing out frequently:

```csharp
// In ZrcSdkController.cs
private const int SSH_TIMEOUT_MS = 5000;  // Default 5 seconds
// Increase if necessary:
private const int SSH_TIMEOUT_MS = 10000; // 10 seconds
```

Then rebuild and deploy new version.

### Status Update Interval

If status updates are lagging behind actual Zoom state:

```csharp
// In zStatus.cs or ZoomRoom.cs
private const int STATUS_POLL_INTERVAL_MS = 1000;  // 1 second polling
// Increase frequency (but beware CPU impact):
private const int STATUS_POLL_INTERVAL_MS = 500;   // 500ms polling
```

## Troubleshooting Deployment Issues

| Problem | Symptom | Solution |
|---------|---------|----------|
| Plugin won't load | PluginStatus shows "Failed" | Check architecture match (32-bit vs 64-bit) |
| Version not in dropdown | Can't see new version in PD Tools | Wait 2-3 minutes for semantic-release, then refresh |
| Deployment times out | Progress stuck at "Installing..." | Restart CP4N, retry from previous version first |
| SSH access denied | Can't SSH to 192.168.100.155 | Verify network connectivity, check credentials |
| Memory usage high | CP4N performance degrades over time | Restart plugin: `PluginStop` → `PluginStart` |
| Camera commands fail | Pan/tilt not responding | Verify Zoom Room SDK connection: `show status zCommand` |
| Presentation not visible to far-end | Content only on local display | Verify using v2.0.1+ (has StartSharingOnlyMeeting fix) |

## Post-Deployment Verification Checklist

After successful deployment, verify:

- [ ] Plugin shows in `PluginStatus` as running
- [ ] PD Tools shows "Online" and healthy
- [ ] No error messages in logs (first 30 seconds after deployment)
- [ ] All test scenarios pass (or document failures)
- [ ] Control system responds to commands within expected latency
- [ ] Zoom Room SDK connected and operational
- [ ] Mobile app can connect and control
- [ ] Presentation mode functions correctly
- [ ] Annotation stream available (if enabled)

## Escalation & Support

### If Testing Fails

1. **Collect diagnostic information**:
   ```bash
   ssh admin@192.168.100.155
   
   # Export logs
   tar -czf /tmp/diagnostic.tar.gz /opt/plugins/epi-bic-zoomroom/logs/
   scp admin@192.168.100.155:/tmp/diagnostic.tar.gz ~/Downloads/
   ```

2. **Report issue to development team**:
   - Plugin version tested
   - Test scenario that failed
   - Reproduction steps
   - Attached diagnostic logs
   - Screenshot of error (if applicable)

3. **Development team actions**:
   - Reproduce issue locally or in test environment
   - Identify root cause in source code
   - Create fix on `csv-zoom-sandbox-v2` branch
   - Trigger new release via semantic-release
   - Deploy patched version and re-test

---

**Last Updated**: August 10, 2026
