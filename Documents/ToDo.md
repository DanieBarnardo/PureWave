# PureWave — Outstanding Implementation Tasks

Derived from the business plan's "not yet implemented" items, confirmed against the current codebase.

---

## High Priority

### WhatsApp CTA Button
The site describes a WhatsApp-first contact process on both the Home and Remote pages, but there is no clickable link or button anywhere. A prospective client has no mechanism to actually start the process from the site.

- Add a WhatsApp link button to the Remote page (primary location)
- Add a WhatsApp link to the Home page remote section
- Add the WhatsApp number to a config/settings location so it can be updated in one place

---

### File Upload Support on Intake Form
`IntakeSubmissionFile.cs` already exists as a model but is completely unwired. No upload UI, no storage, no persistence.

- Add file input to the intake form (room photos, floor plan sketches, existing equipment lists)
- Wire `IntakeSubmissionFile` to the intake submission store
- Store files alongside the intake record (local filesystem or object storage)
- Surface uploaded files in the admin intake viewer

---

## Medium Priority

### Remote Lead Qualification Questions
The intake form's generic freetext fields (primary goals, room dimensions, key challenges) are the only way remote clients can provide context. The Blueprint report template expects structured data: usage split (movies vs music %), ambient light level, and acoustic challenges.

- Add structured fields for remote clients: usage split, ambient light level, acoustic challenge categories
- Consider showing these fields only when service mode is "Remote consultancy"

---

### Portfolio / Case Studies Page
Documented as the primary go-to-market tactic in the business plan. Nothing exists on the site.

- Create a `/portfolio` page showcasing completed rooms and calibration work
- Include before/after descriptions, system configurations, and outcome summaries
- Link from the Home page and Plans page

---

### Smart Home / Automation as a Named Service
Home Assistant automation (Movie Mode, Zigbee/ESP sensors, environmental triggers) is described as a core differentiator in the business documents. On the site it is only an unchecked checkbox in the intake form.

- Add a section or page describing the smart home and automation capability
- Surface it on the Plans page or as a standalone service description
- Connect it to The Director plan as a key deliverable highlight

---

## Lower Priority

### Follow-up Workflow
No mechanism exists to move a remote enquiry from intake submission into a confirmed consultation slot.

- Add a simple status field to intake records in the admin app (New / Contacted / Slot Arranged / Complete)
- Surface this in the admin intake viewer so open enquiries are visible

---

### Local Media Ecosystems
Advice on local network playback (MKV/ISO, NAS, media server setup) is described as a service in the business documents but is not mentioned anywhere on the site.

- Decide whether to surface this as part of an existing plan description or a separate service mention
- At minimum, add it to The Tuning or The Director deliverables if it applies

---

## Not Planned (Acknowledged)

These are documented as deliberate deferrals — only worth revisiting if volume justifies it:

- **Payment collection** — manual WhatsApp-led process is intentional for now
- **Direct slot booking / calendar integration** — deferred until the manual flow becomes too time-consuming
- **WhatsApp automation** — not appropriate at current scale

---

## Factual Corrections Needed in Documents

- **Business Plan** (Section 7): States "PostgreSQL storage" — the implementation uses **MySQL**. Update the document.
- **Pure Wave Plans.md** and **PureWave Business Thoughts.md**: The Tuning plan states "Min 3 hours" minimum booking. The site shows "Hourly rate with minimum booking" without the hours specified. Align these or add the 3-hour minimum back to the site.
