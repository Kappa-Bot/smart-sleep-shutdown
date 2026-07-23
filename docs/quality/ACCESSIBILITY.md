# Accessibility release gate

- Spanish UI text comes from resources and remains readable at 200% DPI.
- Keyboard navigation covers onboarding, shell, tray flyout, and warning.
- Focus is visible and ordered; the cancel action is the warning default.
- Status and countdown changes use polite live announcements at milestones.
- Color is never the only state signal; icon, label, and accessible name agree.
- High contrast and Windows reduced-motion settings are respected.
- One-monitor and mixed-DPI multi-monitor placement must pass manual QA.

Automated tests cover resource presence, state names, warning milestones, and
placement calculations. Narrator, high contrast, and real mixed-DPI behavior
remain hardware/manual gates.
