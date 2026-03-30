![PureWave Logo](D:\Development\PureWave\Documents\Pure Wave.png)

# Business Plan: PureWave Remote Cinema Consultancy

## 1. Executive Summary

PureWave remains a **Durban-first, on-site home theatre consultancy**, with **remote consultancy offered as an additional service track** for clients who are outside Durban or who need planning and procurement help before an on-site visit makes sense.

The remote offer is designed to help clients make better decisions about:
- room layout
- speaker and screen positioning
- wiring priorities
- equipment choices
- upgrade paths for movies and music

The remote model is not positioned as a replacement for on-site tuning, calibration, or installation-led consultancy. Instead, it extends the PureWave brand by making expert planning and independent buying advice available to a wider audience.

## 2. Current Website Positioning

The current PureWave web application has been updated to reflect this dual structure:

- **On-site services remain the flagship offer**
- **Remote consultancy is presented as an additional service**
- the site still leads with Durban-based support for premium home theatre rooms
- remote services are positioned as ideal for planning, guidance, and procurement support

This is now reflected in the public site copy, plans overview, intake form, and remote services page.

## 3. Implemented Remote Service Offerings

The web application currently presents two remote offers:

### A. The Virtual Blueprint

**Focus:** Remote planning help before building or buying

**Use case:** Clients who need help with:
- room layout
- seating positions
- screen sizing
- speaker placement
- wiring priorities

**Deliverable:** A written layout and wiring-priority report, based on the client’s intake, discussion, and remote consultation.

### B. The Procurement Report

**Focus:** Independent equipment and upgrade guidance

**Use case:** Clients who want help choosing the right equipment for:
- movies
- music
- future scalability
- South African pricing and stock context

**Deliverable:** A written research and recommendation report with buying guidance and technical reasoning.

## 4. Implemented Website Changes

The following remote-service changes have already been implemented in the PureWave web application:

### Public Pages

- Added a dedicated **Remote Services** page in the web app
- Updated the **Plans** page to separate:
  - **On-Site**
  - **Remote**
- Updated the **Home** page to introduce remote services without replacing the Durban-first positioning

### Plans Data

The plans data now includes:

**On-Site**
- The Blueprint
- The Tuning
- The Director

**Remote**
- The Virtual Blueprint
- The Procurement Report

### Intake Form

The intake form has been updated so clients can now choose:
- service mode
- service format
- on-site vs remote fit

It also adapts the available choices depending on the selected service mode:

- if a client selects **Remote consultancy**, the form hides options that are only relevant to on-site work
- remote users see only relevant plan, project-stage, and service-interest options

### Database Support

The intake database schema and persistence layer were updated to support the new fields:
- `service_mode`
- `service_format`

These fields are now stored as part of intake submissions.

## 5. Operational Workflow: What Is Now Reflected on the Site

The website no longer suggests that remote clients buy a fixed slot directly through a portal.

Instead, the implemented website now reflects the actual intended remote process:

1. **Client makes contact via WhatsApp**
2. **PureWave explains the remote process**
3. **A remote slot is arranged at the client’s convenience**
4. **The client completes the intake and shares relevant room and equipment details**
5. **PureWave conducts the remote consultation**
6. **A written report is delivered afterward**

This is important because it keeps the process personal, flexible, and practical, rather than forcing a rigid self-service booking flow before trust has been established.

## 6. What The Website Currently Says About Remote Booking

The public site now reflects the following booking logic:

- remote work starts with **WhatsApp contact**
- the consultation slot is **arranged around the client’s convenience**
- the intake still helps PureWave gather the right context before the discussion
- the final output is still a **report-led service**

This is a better fit for the PureWave brand than hard-selling a remote slot through an automated checkout page too early.

## 7. Technology and Workflow Assumptions

The website currently supports:
- remote-service messaging
- a remote services page
- service-mode-aware intake handling
- PostgreSQL storage of updated intake submissions

The website does **not yet fully implement**:
- direct remote slot booking
- WhatsApp automation
- file uploads for room photos and floor plans
- payment collection for remote sessions

These are still possible future improvements if the remote service grows.

## 8. Recommended Next Steps

If the remote service offering grows, the next logical website improvements would be:

1. Add a dedicated **WhatsApp CTA** on the remote page
2. Add **file upload support** for room photos, sketches, and existing equipment lists
3. Add optional **remote-service lead qualification questions**
4. Add a lightweight **follow-up workflow** for moving a remote enquiry into a confirmed consultation slot
5. Later, add payment handling only if the manual WhatsApp-led booking flow becomes too time-consuming

## 9. Strategic Fit

The remote-service implementation now fits the business more accurately than the original remote-first concept:

- PureWave keeps its premium local identity
- remote work broadens reach without weakening the on-site offer
- the site can now speak to both:
  - Durban clients wanting hands-on home theatre help
  - non-local clients wanting expert planning and buying guidance

This keeps the brand grounded in real-world home theatre passion while making the consultancy more scalable over time.
