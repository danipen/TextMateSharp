#include "oniguruma.h"

#if defined(_WIN32)
#define ONIGWRAP_EXTERN extern __declspec(dllexport)
#else
#define ONIGWRAP_EXTERN extern
#endif

ONIGWRAP_EXTERN
regex_t *onigwrap_create(char *pattern, int len, int ignoreCase, int multiline);

ONIGWRAP_EXTERN
OnigRegion* onigwrap_region_new();

ONIGWRAP_EXTERN
void onigwrap_region_free(OnigRegion *region);

ONIGWRAP_EXTERN
void onigwrap_free(regex_t *reg);

ONIGWRAP_EXTERN
int onigwrap_search(regex_t *reg, char *charPtr, int offset, int length, OnigRegion *region);

ONIGWRAP_EXTERN
int onigwrap_num_regs(OnigRegion *region);

ONIGWRAP_EXTERN
int onigwrap_pos(OnigRegion *region, int nth);

ONIGWRAP_EXTERN
int onigwrap_len(OnigRegion *region, int nth);