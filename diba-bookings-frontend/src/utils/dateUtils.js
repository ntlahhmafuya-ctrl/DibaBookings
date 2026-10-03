export const dateText = (value) =>
    value
        ? new Date(value).toLocaleDateString([], {
              day: "2-digit",
              month: "short",
              year: "numeric"
          })
        : "-";

export const timeText = (value) =>
    value
        ? new Date(value).toLocaleTimeString([], {
              hour: "2-digit",
              minute: "2-digit"
          })
        : "-";

export const formatDate = (value) =>
    value
        ? new Date(value).toLocaleString([], {
              day: "2-digit",
              month: "short",
              hour: "2-digit",
              minute: "2-digit"
          })
        : "-";

export const getDate = (item) =>
    item.startDateTime || item.bookingDate || item.date;

export const localInput = (value) =>
    value ? new Date(value).toISOString().slice(0, 16) : "";
