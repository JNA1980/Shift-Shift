#!/bin/sh
# Final step before the Game Off submission (run on the owner's PC, with Unity closed):
#   - every commit's author and committer name becomes the current git name (git config user.name),
#   - three old commit subjects that contain a name are reworded,
#   - every commit is signed with the owner's own signing key.
# It rewrites only the commit records; the working files are never touched, so it cannot trip over LFS files.
# Run from the repository root:   git -c alias.signhistory='!sh tools/sign-history.sh' signhistory
# Then check the output and:      git push --force-with-lease origin main
set -e
BRANCH="${1:-main}"
NAME="$(git config user.name)"
[ -n "$NAME" ] || { echo "git config user.name is empty."; exit 1; }
if ! git diff --quiet || ! git diff --cached --quiet; then
  echo "There are uncommitted changes. Commit or discard them first."; exit 1
fi
echo "Rewriting $BRANCH: name -> $NAME, signing every commit..."
export NAME FILTER_BRANCH_SQUELCH_WARNING=1
git filter-branch -f \
  --env-filter 'GIT_AUTHOR_NAME="$NAME"; GIT_COMMITTER_NAME="$NAME"; export GIT_AUTHOR_NAME GIT_COMMITTER_NAME' \
  --msg-filter 'sed -e "s/^D16: chart editor verified by .*$/D16: chart editor verified/" -e "s/^D10: [A-Z]*\.md, build log/D10: project rules, build log/" -e "s/^D19: turn off .* commit and PR attribution$/D19: turn off commit and PR attribution/"' \
  --commit-filter 'git commit-tree -S "$@"' \
  -- "$BRANCH"
TOTAL=$(git rev-list --count "$BRANCH")
SIGNED=$(git log "$BRANCH" --format=%H | while read h; do git cat-file commit "$h" | grep -q '^gpgsig' && echo x; done | wc -l)
echo
echo "Commits: $TOTAL   signed: $SIGNED"
echo "Author / committer names now in the history:"
git log "$BRANCH" --format='  %an / %cn' | sort | uniq -c
echo "If both numbers match and only your name is listed, push with:"
echo "  git push --force-with-lease origin main"
