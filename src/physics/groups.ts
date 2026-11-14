// Who collides with whom. Rapier packs a group as (membership << 16) | filter.
// Gates and the raised Haram platform stop the car but let a person on foot through or up.
export const CAR_BIT = 0x0001, CAR_ONLY_BIT = 0x0002, WALKER_BIT = 0x0004;
const pack = (membership: number, filter: number) => ((membership << 16) | filter) >>> 0;
export const GROUP_CAR = pack(CAR_BIT, 0xffff);
export const GROUP_CAR_ONLY = pack(CAR_ONLY_BIT, CAR_BIT);
/** Used when the walker asks what blocks it: everything except car-only barriers. */
export const GROUP_WALKER_QUERY = pack(WALKER_BIT, 0xffff & ~CAR_ONLY_BIT);
/** The walker's own collider: nothing is pushed by it. */
export const GROUP_WALKER_SELF = pack(WALKER_BIT, 0);
