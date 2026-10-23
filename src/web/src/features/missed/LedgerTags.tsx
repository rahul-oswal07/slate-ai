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
      className={`inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-label ${className}`}
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
export function ChannelTag({ channel }: { channel: Channel }) {
  const { t } = useTranslation();
  return <Tag {...channels[channel]} text={t(`missed.channel.${channel}`)} />;
}
