import { useTranslation } from "react-i18next";
import { Icon, type IconName } from "../../components/Icon";
import type { Channel, LearningStatus } from "./types";

// D-35 tag colours. "Missed lesson" shares the stopped colours, told apart by its icon and word.
const learning: Record<LearningStatus, { icon: IconName; className: string }> =
  {
    missed: {
      icon: "calendarX",
      className: "bg-status-stopped-bg text-status-stopped",
    },
    sent: { icon: "send", className: "bg-status-sent-bg text-status-sent" },
    caughtUp: {
      icon: "checkCircle",
      className: "bg-status-caught-up-bg text-status-caught-up",
    },
    needsBuddy: {
      icon: "users",
      className: "bg-status-needs-buddy-bg text-status-needs-buddy",
    },
  };

const channels: Record<Channel, { icon: IconName; className: string }> = {
  stopped: {
    icon: "bellOff",
    className: "bg-status-stopped-bg text-status-stopped",
  },
  noPhone: {
    icon: "smartphoneOff",
    className: "bg-status-no-phone-bg text-status-no-phone",
  },
  noConsent: {
    icon: "lock",
    className: "bg-status-queued-bg text-status-queued",
  },
};

function Tag({
  icon,
  className,
  text,
}: {
  icon: IconName;
  className: string;
  text: string;
}) {
  return (
    <span
      className={`inline-flex min-h-8 items-center gap-1.5 rounded-pill px-3 py-0.5 text-caption font-bold ${className}`}
    >
      <Icon name={icon} size={16} />
      {text}
    </span>
  );
}

/** Learning status: always icon + word, never colour alone. */
export function LearningTag({
  status,
  count,
}: {
  status: LearningStatus;
  count?: number;
}) {
  const { t } = useTranslation();
  const text = t(`missed.status.${status}`);
  return (
    <Tag
      {...learning[status]}
      text={count === undefined ? text : `${count} ${text}`}
    />
  );
}

/** The family's channel, a separate tag so a stop never hides "Caught up" (B-08). */
export function ChannelTag({
  channel,
  compact = false,
}: {
  channel: Channel;
  /** A small icon + word line under a name in a table row (T-98), not a pill. */
  compact?: boolean;
}) {
  const { t } = useTranslation();
  const text = t(`missed.channel.${channel}`);
  if (compact) {
    return (
      <span className="flex items-start gap-1 pb-1 text-caption text-ink-muted">
        <Icon name={channels[channel].icon} size={14} className="mt-[3px] shrink-0" />
        {text}
      </span>
    );
  }
  return <Tag {...channels[channel]} text={text} />;
}

/** A missed lesson whose catch-up hasn't gone yet (no level): one chip on the child view, icon + name + word (T-98). */
export function MissedLessonChip({ lesson }: { lesson: string }) {
  const { t } = useTranslation();
  return (
    <span className="inline-flex items-center gap-1 rounded-pill border-[1.5px] border-status-stopped bg-status-stopped-bg px-2 py-0.5 text-caption font-bold text-status-stopped">
      <Icon name={learning.missed.icon} size={14} className="shrink-0" />
      {`${lesson} · ${t("missed.status.missed")}`}
    </span>
  );
}
